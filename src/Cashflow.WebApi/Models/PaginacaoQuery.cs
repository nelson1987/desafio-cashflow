using Microsoft.AspNetCore.Mvc;

namespace Cashflow.WebApi.Models;

/// <summary>
/// Parâmetros de paginação para listagem
/// </summary>
public record PaginacaoQuery
{
    /// <summary>
    /// Número da página (padrão: 1)
    /// </summary>
    [FromQuery(Name = "pagina")]
    public int Pagina { get; init; } = 1;

    /// <summary>
    /// Quantidade de itens por página (padrão: 10, máximo: 100)
    /// </summary>
    [FromQuery(Name = "tamanhoPagina")]
    public int TamanhoPagina { get; init; } = 10;
}

