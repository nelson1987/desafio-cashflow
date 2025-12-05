using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace Cashflow.WebApi.Extensions;

/// <summary>
/// Extensões para configurar observabilidade (OpenTelemetry + Serilog)
/// </summary>
public static class ObservabilityExtensions
{
    private const string ServiceName = "cashflow-api";
    private const string ServiceVersion = "1.0.0";

    /// <summary>
    /// Adiciona OpenTelemetry com Jaeger (traces) e Prometheus (métricas)
    /// </summary>
    public static IServiceCollection AddObservability(
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
                    ["environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/health") &&
                        !context.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddSource(ServiceName)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(ServiceName)
                .AddPrometheusExporter());

        return services;
    }

    /// <summary>
    /// Configura o Serilog com output para Console e Loki
    /// </summary>
    public static IHostBuilder ConfigureSerilog(
        this IHostBuilder hostBuilder,
        IConfiguration configuration)
    {
        var lokiUrl = configuration["Observability:LokiUrl"] ?? "http://localhost:3100";

        return hostBuilder.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
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
                        new() { Key = "env", Value = context.HostingEnvironment.EnvironmentName }
                    },
                    propertiesAsLabels: new[] { "level" });
        });
    }

    /// <summary>
    /// Mapeia o endpoint de métricas do Prometheus
    /// </summary>
    public static IEndpointRouteBuilder MapObservabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPrometheusScrapingEndpoint("/metrics");
        return endpoints;
    }
}

