using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.WebApi.Extensions;

namespace Cashflow.WebApi.Endpoints.Lancamentos;

/// <summary>
/// Endpoint para criação de lançamento
/// </summary>
public class CriarLancamentoEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/lancamentos", HandleAsync)
            .WithName("CriarLancamento")
            .WithTags("Lançamentos")
            .WithSummary("Cria um novo lançamento")
            .WithDescription("Registra um novo lançamento de crédito ou débito no fluxo de caixa")
            .Produces<LancamentoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        CriarLancamentoRequest request,
        ILancamentoService service,
        CancellationToken ct)
    {
        var result = await service.CriarAsync(request, ct);
        return result.ToCreatedResult($"/api/lancamentos/{result.Value?.Id}");
    }
}