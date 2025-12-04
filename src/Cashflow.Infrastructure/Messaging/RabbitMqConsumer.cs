using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cashflow.Infrastructure.Messaging;

/// <summary>
/// Classe base para consumidores de mensagens RabbitMQ
/// </summary>
public abstract class RabbitMqConsumerBase<TMessage> : BackgroundService where TMessage : class
{
    protected readonly RabbitMqSettings Settings;
    protected readonly ILogger Logger;
    protected readonly JsonSerializerOptions JsonOptions;

    private IConnection? _connection;
    private IChannel? _channel;

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    protected RabbitMqConsumerBase(
        IOptions<RabbitMqSettings> settings,
        ILogger logger)
    {
        Settings = settings.Value;
        Logger = logger;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        await InitializeAsync(stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(messageJson, JsonOptions);

                if (message != null)
                {
                    Logger.LogDebug(
                        "Mensagem recebida. Queue: {Queue}, MessageId: {MessageId}",
                        QueueName,
                        ea.BasicProperties?.MessageId);

                    await ProcessMessageAsync(message, stoppingToken);

                    // Confirma o processamento
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                }
                else
                {
                    Logger.LogWarning(
                        "Mensagem deserializada como null. Queue: {Queue}",
                        QueueName);

                    // Rejeita a mensagem sem requeue
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Erro ao processar mensagem. Queue: {Queue}, Body: {Body}",
                    QueueName,
                    messageJson);

                // Reenfileira a mensagem para retry
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        Logger.LogInformation("Consumidor iniciado. Queue: {Queue}", QueueName);

        // Mantém o serviço rodando
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = Settings.Host,
            Port = Settings.Port,
            UserName = Settings.Username,
            Password = Settings.Password,
            VirtualHost = Settings.VirtualHost,
            AutomaticRecoveryEnabled = Settings.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(Settings.NetworkRecoveryInterval)
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Configura prefetch para processar uma mensagem por vez
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        // Declara a exchange
        await _channel.ExchangeDeclareAsync(
            exchange: Settings.Exchange,
            type: Settings.ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Declara a fila
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", $"{Settings.Exchange}.dlx" },
                { "x-dead-letter-routing-key", $"{RoutingKey}.dead" }
            },
            cancellationToken: cancellationToken);

        // Faz o bind da fila com a exchange
        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: Settings.Exchange,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        Logger.LogInformation(
            "Fila configurada. Queue: {Queue}, RoutingKey: {RoutingKey}",
            QueueName,
            RoutingKey);
    }

    /// <summary>
    /// Processa a mensagem recebida
    /// </summary>
    protected abstract Task ProcessMessageAsync(TMessage message, CancellationToken cancellationToken);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Parando consumidor. Queue: {Queue}", QueueName);

        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}