using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

using Testcontainers.Redis;

namespace Cashflow.IntegrationTests.Fixtures;

/// <summary>
/// Fixture para container Redis usando Testcontainers
/// </summary>
public class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container;
    private ConnectionMultiplexer? _connection;

    public RedisContainerFixture()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public ConnectionMultiplexer? Connection => _connection;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        await _container.DisposeAsync();
    }

    public IDistributedCache CreateDistributedCache()
    {
        var options = Options.Create(new RedisCacheOptions
        {
            Configuration = ConnectionString,
            InstanceName = "test:"
        });

        return new RedisCache(options);
    }

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = ConnectionString;
            options.InstanceName = "test:";
        });

        return services;
    }

    public async Task FlushDatabaseAsync()
    {
        if (_connection != null)
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            await server.FlushDatabaseAsync();
        }
    }
}

/// <summary>
/// Collection definition para compartilhar o container entre testes
/// </summary>
[CollectionDefinition(Name)]
public class RedisCollection : ICollectionFixture<RedisContainerFixture>
{
    public const string Name = "Redis";
}

