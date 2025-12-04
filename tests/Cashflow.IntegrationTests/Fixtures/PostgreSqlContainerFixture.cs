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
        
        // Cria o schema e tabelas
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        await InitializeSchemaAsync();
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

    private async Task InitializeSchemaAsync()
    {
        const string initSql = """
            CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
            
            CREATE SCHEMA IF NOT EXISTS cashflow;
            
            CREATE TABLE IF NOT EXISTS cashflow.lancamentos (
                id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                valor DECIMAL(18, 2) NOT NULL CHECK (valor > 0),
                tipo SMALLINT NOT NULL CHECK (tipo IN (1, 2)),
                data TIMESTAMP NOT NULL,
                descricao VARCHAR(500) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            
            CREATE TABLE IF NOT EXISTS cashflow.saldos_consolidados (
                data DATE PRIMARY KEY,
                total_creditos DECIMAL(18, 2) NOT NULL DEFAULT 0,
                total_debitos DECIMAL(18, 2) NOT NULL DEFAULT 0,
                saldo DECIMAL(18, 2) NOT NULL DEFAULT 0,
                quantidade_lancamentos INT NOT NULL DEFAULT 0,
                processado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            
            CREATE INDEX IF NOT EXISTS idx_lancamentos_data ON cashflow.lancamentos(data);
            CREATE INDEX IF NOT EXISTS idx_lancamentos_tipo ON cashflow.lancamentos(tipo);
            """;

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(initSql);
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

