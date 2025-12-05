using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;

namespace Cashflow.WebApi.Endpoints.Consolidado;

/// <summary>
/// Endpoint para obter saldo consolidado por data
/// </summary>
public class ObterConsolidadoPorDataEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/consolidado/{data:datetime}", HandleAsync)
            .WithName("ObterConsolidadoPorData")
            .WithTags("Consolidado")
            .WithSummary("Obtém o saldo consolidado de uma data")
            .WithDescription("Retorna o saldo consolidado (créditos, débitos e saldo) de uma data específica")
            .Produces<SaldoConsolidadoResponse>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        DateTime data,
        IConsolidadoService service,
        CancellationToken ct)
    {
        var result = await service.ObterPorDataAsync(data, ct);
        return result.ToHttpResult();
    }
}