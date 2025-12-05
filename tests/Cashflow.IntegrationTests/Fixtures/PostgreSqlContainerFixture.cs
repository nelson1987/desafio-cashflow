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

        // Cria a extensão uuid-ossp ANTES de criar o schema
        // pois as tabelas usam uuid_generate_v4() como valor padrão
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
            CREATE SCHEMA IF NOT EXISTS cashflow;
            """);
        
        // Agora cria as tabelas (a extensão já existe)
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