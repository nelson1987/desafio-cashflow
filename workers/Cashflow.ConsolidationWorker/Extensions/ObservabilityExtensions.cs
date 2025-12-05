using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace Cashflow.ConsolidationWorker.Extensions;

/// <summary>
/// Extensões para configurar observabilidade no Worker
/// </summary>
public static class ObservabilityExtensions
{
    private const string ServiceName = "cashflow-worker";
    private const string ServiceVersion = "1.0.0";

    /// <summary>
    /// Adiciona OpenTelemetry com Jaeger (traces)
    /// </summary>
    public static IServiceCollection AddWorkerObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: ServiceName,
                    serviceVersion: ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["DOTNET_ENVIRONMENT"] ?? "Development",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddSource(ServiceName)
                .AddSource("Cashflow.Worker")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }));

        return services;
    }

    /// <summary>
    /// Configura o Serilog com output para Console e Loki
    /// </summary>
    public static LoggerConfiguration ConfigureWorkerSerilog(
        this LoggerConfiguration loggerConfig,
        IConfiguration configuration)
    {
        var lokiUrl = configuration["Observability:LokiUrl"] ?? "http://localhost:3100";
        var environment = configuration["DOTNET_ENVIRONMENT"] ?? "Development";

        return loggerConfig
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", ServiceName)
            .Enrich.WithProperty("Version", ServiceVersion)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.GrafanaLoki(
                lokiUrl,
                labels: new List<LokiLabel>
                {
                    new() { Key = "app", Value = ServiceName },
                    new() { Key = "env", Value = environment }
                },
                propertiesAsLabels: new[] { "level" });
    }
}