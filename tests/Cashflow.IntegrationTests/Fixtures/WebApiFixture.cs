using Cashflow.Abstractions;
using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Repositories;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cashflow.IntegrationTests.Fixtures;

/// <summary>
/// Fixture para testes de integração da Web API
/// </summary>
public class WebApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public IntegrationTestFixture Infrastructure { get; } = new();

    public HttpClient Client => _client ?? throw new InvalidOperationException("Client não inicializado");
    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Factory não inicializada");

    public async Task InitializeAsync()
    {
        // Inicia os containers de infraestrutura
        await Infrastructure.InitializeAsync();

        // Cria a WebApplicationFactory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                // Configura as connection strings e settings para usar os containers de teste
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        // Connection strings
                        ["ConnectionStrings:PostgreSQL"] = Infrastructure.PostgreSql.ConnectionString,
                        ["ConnectionStrings:Redis"] = Infrastructure.Redis.ConnectionString,
                        
                        // RabbitMQ settings
                        ["RabbitMQ:Host"] = Infrastructure.RabbitMq.Host,
                        ["RabbitMQ:Port"] = Infrastructure.RabbitMq.Port.ToString(),
                        ["RabbitMQ:Username"] = Infrastructure.RabbitMq.Username,
                        ["RabbitMQ:Password"] = Infrastructure.RabbitMq.Password,
                        ["RabbitMQ:VirtualHost"] = "/",
                        ["RabbitMQ:Exchange"] = RabbitMqContainerFixture.TestExchange
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Remove o DbContext original e outros serviços que serão reconfigurados
                    services.RemoveAll<DbContextOptions<CashflowDbContext>>();
                    services.RemoveAll<CashflowDbContext>();
                    
                    // Remove os repositórios originais (eles dependem do DbContext antigo)
                    services.RemoveAll<ILancamentoRepository>();
                    services.RemoveAll<ISaldoConsolidadoRepository>();

                    // Configura os serviços com os containers de teste (inclui DbContext, Redis, RabbitMQ)
                    Infrastructure.ConfigureServices(services);
                    
                    // Re-registra os repositórios para usar o novo DbContext
                    services.AddScoped<ILancamentoRepository, LancamentoRepository>();
                    services.AddScoped<ISaldoConsolidadoRepository, SaldoConsolidadoRepository>();

                    // Remove os health checks originais e adiciona novos com as configurações de teste
                    var healthCheckDescriptors = services
                        .Where(s => s.ServiceType == typeof(HealthCheckService) ||
                                    s.ServiceType.FullName?.Contains("HealthCheck") == true)
                        .ToList();

                    foreach (var descriptor in healthCheckDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Adiciona health checks apontando para os containers de teste
                    services.AddHealthChecks()
                        .AddNpgSql(Infrastructure.PostgreSql.ConnectionString, name: "postgresql-test")
                        .AddRedis(Infrastructure.Redis.ConnectionString, name: "redis-test");
                });
            });

        _client = _factory.CreateClient();

        // Aplica as migrations no banco de teste
        await ApplyMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        await Infrastructure.DisposeAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CashflowDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Limpa os dados do banco para cada teste
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        try
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CashflowDbContext>();
            
            // Verifica se as tabelas existem antes de truncar
            var tableExists = await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'cashflow' AND table_name = 'lancamentos') THEN
                        TRUNCATE TABLE cashflow.lancamentos, cashflow.saldos_consolidados CASCADE;
                    END IF;
                END $$;
            ");
        }
        catch (Exception ex)
        {
            // Log para debug - não propaga o erro para não quebrar os testes
            Console.WriteLine($"ResetDatabaseAsync warning: {ex.Message}");
        }
    }
}

/// <summary>
/// Collection definition para testes de API
/// </summary>
[CollectionDefinition(Name)]
public class WebApiTestCollection : ICollectionFixture<WebApiFixture>
{
    public const string Name = "WebApiTests";
}

