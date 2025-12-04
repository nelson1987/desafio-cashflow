using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;

namespace Cashflow.WebApi.Endpoints.Consolidado;

/// <summary>
/// Endpoint para recalcular saldo consolidado
/// </summary>
public class RecalcularConsolidadoEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/consolidado/{data:datetime}/recalcular", HandleAsync)
            .WithName("RecalcularConsolidado")
            .WithTags("Consolidado")
            .WithSummary("Recalcula o saldo consolidado de uma data")
            .WithDescription("Força o recálculo do saldo consolidado baseado nos lançamentos da data")
            .Produces<SaldoConsolidadoResponse>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        DateTime data,
        IConsolidadoService service,
        CancellationToken ct)
    {
        var result = await service.RecalcularAsync(data, ct);
        return result.ToHttpResult();
    }
}

