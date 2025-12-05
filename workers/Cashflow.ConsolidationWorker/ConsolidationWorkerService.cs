using System.Text;
using System.Text.Json;
using Cashflow.Application.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cashflow.ConsolidationWorker;

/// <summary>
/// Worker service que consome eventos de lançamentos do RabbitMQ
/// e processa a consolidação de saldos diários.
/// </summary>
public class ConsolidationWorkerService : BackgroundService
{
    private readonly ILogger<ConsolidationWorkerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConsumerSettings _settings;
    private readonly ResiliencePipeline _resiliencePipeline;
    private IConnection? _connection;
    private IChannel? _channel;

    public ConsolidationWorkerService(
        ILogger<ConsolidationWorkerService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqConsumerSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Tentativa {Attempt} de processar mensagem após erro: {Error}",
                        args.AttemptNumber,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de consolidação iniciado");

        await ConnectAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _logger.LogWarning("Conexão perdida, reconectando...");
                    await ConnectAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no loop principal do worker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ConnectAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Declara a exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // Declara a fila
            await _channel.QueueDeclareAsync(
                queue: _settings.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            // Bind da fila com a routing key
            await _channel.QueueBindAsync(
                queue: _settings.Queue,
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
                cancellationToken: stoppingToken);

            // Configura QoS para processar uma mensagem por vez
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

            // Configura o consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(
                queue: _settings.Queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Conectado ao RabbitMQ. Host: {Host}, Exchange: {Exchange}, Queue: {Queue}",
                _settings.Host,
                _settings.Exchange,
                _settings.Queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar ao RabbitMQ");
            throw;
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        _logger.LogInformation("Mensagem recebida: {Message}", message);

        try
        {
            await _resiliencePipeline.ExecuteAsync(async token =>
            {
                await ProcessMessageAsync(message);
            });

            // Acknowledge da mensagem
            if (_channel != null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Mensagem processada com sucesso");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem após todas as tentativas");

            // Reject da mensagem (vai para DLQ se configurada)
            if (_channel != null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        var evento = JsonSerializer.Deserialize<LancamentoEventMessage>(message, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evento == null)
        {
            _logger.LogWarning("Mensagem inválida recebida: {Message}", message);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var consolidadoService = scope.ServiceProvider.GetRequiredService<IConsolidadoService>();

        _logger.LogInformation(
            "Processando consolidação para data: {Data}, LancamentoId: {LancamentoId}",
            evento.Data.Date,
            evento.LancamentoId);

        await consolidadoService.RecalcularAsync(evento.Data.Date);

        _logger.LogInformation("Consolidação concluída para data: {Data}", evento.Data.Date);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de consolidação parando...");

        if (_channel != null)
        {
            await _channel.CloseAsync(stoppingToken);
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(stoppingToken);
            _connection.Dispose();
        }

        await base.StopAsync(stoppingToken);
    }
}

/// <summary>
/// Representa um evento de lançamento recebido da fila.
/// </summary>
public record LancamentoEventMessage(
    Guid LancamentoId,
    DateTime Data,
    decimal Valor,
    string Tipo,
    string Descricao,
    DateTime Timestamp);

