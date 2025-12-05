using System.Reflection;

using Cashflow.WebApi.Endpoints;

namespace Cashflow.WebApi.Extensions;

/// <summary>
/// Extensões para registro automático de endpoints
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Registra todos os endpoints que implementam IEndpoint
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            var mapMethod = type.GetMethod(nameof(IEndpoint.Map), BindingFlags.Static | BindingFlags.Public);
            mapMethod?.Invoke(null, [app]);
        }

        return app;
    }
}