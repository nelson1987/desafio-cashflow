using Cashflow.Application.Abstractions;
using Cashflow.Application.Services;
using Cashflow.ConsolidationWorker;
using Cashflow.ConsolidationWorker.Extensions;
using Cashflow.ConsolidationWorker.Services;
using Cashflow.Infrastructure;

using Serilog;

// Configuração do Serilog
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ConfigureWorkerSerilog(configuration)
    .CreateLogger();

try
{
    Log.Information("Iniciando Cashflow Consolidation Worker...");

    var builder = Host.CreateApplicationBuilder(args);

    // Serilog
    builder.Services.AddSerilog();

    // Configuração do RabbitMQ
    builder.Services.Configure<RabbitMqConsumerSettings>(
        builder.Configuration.GetSection("RabbitMQ"));

    // Infrastructure (PostgreSQL, Redis, RabbitMQ)
    builder.Services.AddInfrastructure(builder.Configuration);

    // OpenTelemetry (Jaeger)
    builder.Services.AddWorkerObservability(builder.Configuration);

    // Application Services
    builder.Services.AddScoped<IConsolidadoService, ConsolidadoService>();

    // Background Service para consumir mensagens
    builder.Services.AddHostedService<ConsolidationWorkerService>();

    // Health check via arquivo
    builder.Services.AddHostedService<HealthCheckFileService>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker encerrado inesperadamente");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
