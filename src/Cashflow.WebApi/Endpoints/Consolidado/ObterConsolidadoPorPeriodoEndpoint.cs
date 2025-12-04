using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;
using Cashflow.WebApi.Models;

namespace Cashflow.WebApi.Endpoints.Consolidado;

/// <summary>
/// Endpoint para obter relatório consolidado por período
/// </summary>
public class ObterConsolidadoPorPeriodoEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/consolidado/periodo", HandleAsync)
            .WithName("ObterConsolidadoPorPeriodo")
            .WithTags("Consolidado")
            .WithSummary("Obtém relatório consolidado por período")
            .WithDescription("Retorna o relatório consolidado com saldos diários e resumo do período (máximo 90 dias)")
            .Produces<RelatorioConsolidadoResponse>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PeriodoQuery query,
        IConsolidadoService service,
        CancellationToken ct)
    {
        var result = await service.ObterPorPeriodoAsync(query.DataInicio, query.DataFim, ct);
        return result.ToHttpResult();
    }
}

