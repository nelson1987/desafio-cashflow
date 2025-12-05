using Cashflow.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace Cashflow.IntegrationTests.Fixtures;

/// <summary>
/// Fixture para container PostgreSQL usando Testcontainers
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("cashflow_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Cria o schema antes das tabelas
        // gen_random_uuid() é nativo do PostgreSQL 13+ (não precisa de extensão)
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE SCHEMA IF NOT EXISTS cashflow;
            """);

        // Cria as tabelas
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public CashflowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CashflowDbContext>()
            .UseNpgsql(ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
            })
            .Options;

        return new CashflowDbContext(options);
    }

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<CashflowDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
            });
        });

        return services;
    }
}

/// <summary>
/// Collection definition para compartilhar o container entre testes
/// </summary>
[CollectionDefinition(Name)]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "PostgreSQL";
}