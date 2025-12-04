using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;

namespace Cashflow.WebApi.Endpoints.Lancamentos;

/// <summary>
/// Endpoint para obter lançamentos por data
/// </summary>
public class ObterLancamentosPorDataEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lancamentos/data/{data:datetime}", HandleAsync)
            .WithName("ObterLancamentosPorData")
            .WithTags("Lançamentos")
            .WithSummary("Obtém lançamentos de uma data específica")
            .WithDescription("Retorna todos os lançamentos registrados em uma data")
            .Produces<IEnumerable<LancamentoResponse>>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        DateTime data,
        ILancamentoService service,
        CancellationToken ct)
    {
        var result = await service.ObterPorDataAsync(data, ct);
        return result.ToHttpResult();
    }
}

