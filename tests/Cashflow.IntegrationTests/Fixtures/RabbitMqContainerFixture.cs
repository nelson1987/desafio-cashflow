using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Testcontainers.RabbitMq;

namespace Cashflow.IntegrationTests.Fixtures;

/// <summary>
/// Fixture para container RabbitMQ usando Testcontainers
/// </summary>
public class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;
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

        var json = JsonSerializer.Serialize(message);
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

        var tcs = new TaskCompletionSource<T?>();
        using var cts = new CancellationTokenSource(timeoutMs);

        cts.Token.Register(() => tcs.TrySetResult(null));

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<T>(json);

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
            tcs.TrySetResult(message);
        };

        await _channel.BasicConsumeAsync(
            queue: TestQueue,
            autoAck: false,
            consumer: consumer);

        return await tcs.Task;
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
}

/// <summary>
/// Collection definition para compartilhar o container entre testes
/// </summary>
[CollectionDefinition(Name)]
public class RabbitMqCollection : ICollectionFixture<RabbitMqContainerFixture>
{
    public const string Name = "RabbitMQ";
}