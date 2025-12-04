using System.Text;
using System.Text.Json;

using Cashflow.Abstractions;
using Cashflow.Infrastructure.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

using RabbitMQ.Client;

namespace Cashflow.Infrastructure.Messaging;

/// <summary>
/// Implementação do publicador de mensagens usando RabbitMQ
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var circuitBreakerDuration = TimeSpan.FromSeconds(InfrastructureSettings.Resilience.CircuitBreakerDurationSeconds);

        // Configura pipeline de resiliência
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = InfrastructureSettings.Resilience.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(InfrastructureSettings.Resilience.PublisherRetryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Tentativa {Attempt} de publicação falhou. Tentando novamente em {Delay}ms",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = InfrastructureSettings.Resilience.CircuitBreakerFailureRatio,
                MinimumThroughput = InfrastructureSettings.Resilience.MessagingMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(InfrastructureSettings.Resilience.SamplingDurationSeconds),
                BreakDuration = circuitBreakerDuration,
                OnOpened = args =>
                {
                    _logger.LogWarning("Circuit breaker ABERTO para RabbitMQ");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker FECHADO para RabbitMQ");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(InfrastructureSettings.Resilience.MessagingTimeoutSeconds))
            .Build();
    }

    public async Task PublicarAsync<T>(T mensagem, CancellationToken cancellationToken = default) where T : class
    {
        var routingKey = GetRoutingKey<T>();
        await PublicarAsync(routingKey, mensagem, cancellationToken);
    }

    public async Task PublicarAsync<T>(string topico, T mensagem, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await EnsureConnectionAsync(ct);

                var json = JsonSerializer.Serialize(mensagem, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Type = typeof(T).Name
                };

                await _channel!.BasicPublishAsync(
                    exchange: _settings.Exchange,
                    routingKey: topico,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);

                _logger.LogDebug(
                    "Mensagem publicada. Exchange: {Exchange}, RoutingKey: {RoutingKey}, Type: {Type}",
                    _settings.Exchange,
                    topico,
                    typeof(T).Name);

            }, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError(
                "Circuit breaker aberto. Mensagem não publicada. Tipo: {Type}",
                typeof(T).Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao publicar mensagem. Tipo: {Type}, Tópico: {Topico}",
                typeof(T).Name,
                topico);
            throw;
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            // Fecha conexões existentes
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

            // Cria nova conexão
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = _settings.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_settings.NetworkRecoveryInterval)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declara a exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: _settings.ExchangeType,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Conexão com RabbitMQ estabelecida. Host: {Host}:{Port}",
                _settings.Host,
                _settings.Port);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private static string GetRoutingKey<T>()
    {
        return typeof(T).Name switch
        {
            "LancamentoCriadoEvent" => RoutingKeys.LancamentoCriado,
            "SaldoRecalculadoEvent" => RoutingKeys.SaldoRecalculado,
            _ => typeof(T).Name.ToLowerInvariant()
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _connectionLock.Dispose();

        GC.SuppressFinalize(this);
    }
}