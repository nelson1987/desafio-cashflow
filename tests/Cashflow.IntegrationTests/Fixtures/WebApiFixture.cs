using Cashflow.Infrastructure.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

                builder.ConfigureServices(services =>
                {
                    // Remove o DbContext original
                    services.RemoveAll<DbContextOptions<CashflowDbContext>>();
                    services.RemoveAll<CashflowDbContext>();

                    // Configura os serviços com os containers de teste
                    Infrastructure.ConfigureServices(services);

                    // Adiciona o DbContext com a connection string do container
                    services.AddDbContext<CashflowDbContext>(options =>
                    {
                        options.UseNpgsql(Infrastructure.PostgreSql.ConnectionString);
                    });
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
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CashflowDbContext>();
        
        // Limpa as tabelas (com schema cashflow)
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE cashflow.lancamentos, cashflow.saldos_consolidados CASCADE");
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

