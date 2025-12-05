using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.IntegrationTests.Fixtures;

/// <summary>
/// Fixture completa para testes de integração com todos os containers
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgreSqlContainerFixture PostgreSql { get; } = new();
    public RedisContainerFixture Redis { get; } = new();
    public RabbitMqContainerFixture RabbitMq { get; } = new();

    public async Task InitializeAsync()
    {
        // Inicia todos os containers em paralelo
        await Task.WhenAll(
            PostgreSql.InitializeAsync(),
            Redis.InitializeAsync(),
            RabbitMq.InitializeAsync());
    }

    public async Task DisposeAsync()
    {
        // Finaliza todos os containers
        await PostgreSql.DisposeAsync();
        await Redis.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        PostgreSql.ConfigureServices(services);
        Redis.ConfigureServices(services);
        RabbitMq.ConfigureServices(services);
        return services;
    }
}

/// <summary>
/// Collection definition para compartilhar todos os containers entre testes
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "IntegrationTests";
}