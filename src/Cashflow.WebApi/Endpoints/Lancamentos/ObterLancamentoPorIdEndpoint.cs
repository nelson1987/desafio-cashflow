using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;

namespace Cashflow.WebApi.Endpoints.Lancamentos;

/// <summary>
/// Endpoint para obter lançamento por ID
/// </summary>
public class ObterLancamentoPorIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lancamentos/{id:guid}", HandleAsync)
            .WithName("ObterLancamentoPorId")
            .WithTags("Lançamentos")
            .WithSummary("Obtém um lançamento pelo ID")
            .WithDescription("Retorna os detalhes de um lançamento específico")
            .Produces<LancamentoResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ILancamentoService service,
        CancellationToken ct)
    {
        var result = await service.ObterPorIdAsync(id, ct);
        return result.ToHttpResultWithNotFound();
    }
}

