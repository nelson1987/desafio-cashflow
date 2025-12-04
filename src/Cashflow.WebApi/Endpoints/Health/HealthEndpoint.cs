namespace Cashflow.WebApi.Endpoints.Health;

/// <summary>
/// Endpoint de health check
/// </summary>
public class HealthEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health")
            .WithName("Health")
            .WithTags("Health")
            .WithSummary("Health check da aplicação")
            .WithDescription("Verifica o status de saúde da aplicação e suas dependências");
    }
}

