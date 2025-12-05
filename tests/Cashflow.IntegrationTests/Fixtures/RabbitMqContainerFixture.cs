using System.Text;
using System.Text.Json;

using Cashflow.Abstractions;
using Cashflow.Infrastructure.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RabbitMQ.Client;

using Testcontainers.RabbitMq;

namespace Cashflow.IntegrationTests.Fixtures;

/// <summary>
/// Fixture para container RabbitMQ usando Testcontainers
/// </summary>
public class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;
    private readonly JsonSerializerOptions _jsonOptions;
    private IConnection? _connection;
    private IChannel? _channel;

    public const string TestExchange = "cashflow.events.test";
    public const string TestQueue = "cashflow.queue.test";

    public RabbitMqContainerFixture()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();

        // Usa as mesmas opções de serialização que o RabbitMqPublisher
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public string ConnectionString => _container.GetConnectionString();
    public string Host => _container.Hostname;
    public int Port => _container.GetMappedPublicPort(5672);
    public string Username => "test";
    public string Password => "test";

    public IChannel? Channel => _channel;
    public IConnection? Connection => _connection;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var factory = new ConnectionFactory
        {
            HostName = Host,
            Port = Port,
            UserName = Username,
            Password = Password
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Declara exchange e queue para testes
        await _channel.ExchangeDeclareAsync(
            exchange: TestExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: TestQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await _channel.QueueBindAsync(
            queue: TestQueue,
            exchange: TestExchange,
            routingKey: "#");
    }

    public async Task DisposeAsync()
    {
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

        await _container.DisposeAsync();
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not initialized");

        return await _connection.CreateChannelAsync();
    }

    public async Task PublishMessageAsync<T>(string routingKey, T message) where T : class
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        await _channel.BasicPublishAsync(
            exchange: TestExchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async Task<T?> ConsumeMessageAsync<T>(int timeoutMs = 5000) where T : class
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        // Usa BasicGetAsync para consumir uma única mensagem em vez de registrar um consumer
        // Isso evita problemas de múltiplos consumers e race conditions
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var result = await _channel.BasicGetAsync(TestQueue, autoAck: false);

            if (result != null)
            {
                var body = result.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                await _channel.BasicAckAsync(result.DeliveryTag, false);
                return message;
            }

            // Pequeno delay antes de tentar novamente
            await Task.Delay(50);
        }

        return null;
    }

    public async Task PurgeQueueAsync()
    {
        if (_channel != null)
        {
            await _channel.QueuePurgeAsync(TestQueue);
        }
    }

    public ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = Host,
            Port = Port,
            UserName = Username,
            Password = Password
        };
    }

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Remove o publisher original
        services.RemoveAll<IMessagePublisher>();

        // Configura as settings do RabbitMQ com os dados do container de teste
        services.Configure<RabbitMqSettings>(opts =>
        {
            opts.Host = Host;
            opts.Port = Port;
            opts.Username = Username;
            opts.Password = Password;
            opts.VirtualHost = "/";
            opts.Exchange = TestExchange;
            opts.ExchangeType = "topic";
        });

        // Re-registra o publisher com as novas configurações
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return services;
    }
}

/// <summary>
/// Collection definition para compartilhar o container entre testes
/// </summary>
[CollectionDefinition(Name)]
public class RabbitMqCollection : ICollectionFixture<RabbitMqContainerFixture>
{
    public const string Name = "RabbitMQ";
}