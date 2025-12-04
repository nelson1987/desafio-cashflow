using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;
using Cashflow.WebApi.Models;

namespace Cashflow.WebApi.Endpoints.Lancamentos;

/// <summary>
/// Endpoint para listagem de lançamentos
/// </summary>
public class ListarLancamentosEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lancamentos", HandleAsync)
            .WithName("ListarLancamentos")
            .WithTags("Lançamentos")
            .WithSummary("Lista lançamentos com paginação")
            .WithDescription("Retorna uma lista paginada de todos os lançamentos")
            .Produces<LancamentosListResponse>()
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PaginacaoQuery query,
        ILancamentoService service,
        CancellationToken ct)
    {
        var result = await service.ListarAsync(query.Pagina, query.TamanhoPagina, ct);
        return result.ToHttpResult();
    }
}

