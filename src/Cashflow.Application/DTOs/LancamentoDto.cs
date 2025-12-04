namespace Cashflow.Application.DTOs;

/// <summary>
/// DTO para criação de lançamento
/// </summary>
public record CriarLancamentoRequest
{
    /// <summary>
    /// Valor do lançamento (deve ser maior que zero)
    /// </summary>
    public decimal Valor { get; init; }

    /// <summary>
    /// Tipo do lançamento (Credito = <see cref="TipoLancamento.Credito"/>, Debito = <see cref="TipoLancamento.Debito"/>)
    /// </summary>
    public TipoLancamento Tipo { get; init; }

    /// <summary>
    /// Data do lançamento
    /// </summary>
    public DateTime Data { get; init; }

    /// <summary>
    /// Descrição do lançamento
    /// </summary>
    public string Descricao { get; init; } = string.Empty;
}

/// <summary>
/// DTO de resposta de lançamento
/// </summary>
public record LancamentoResponse
{
    public Guid Id { get; init; }
    public decimal Valor { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public DateTime Data { get; init; }
    public string Descricao { get; init; } = string.Empty;

    public static LancamentoResponse FromDomain(Lancamento lancamento) => new()
    {
        Id = lancamento.Id,
        Valor = lancamento.Valor,
        Tipo = lancamento.Tipo.ToString(),
        Data = lancamento.Data,
        Descricao = lancamento.Descricao
    };
}

/// <summary>
/// DTO para listagem paginada de lançamentos
/// </summary>
public record LancamentosListResponse
{
    public IEnumerable<LancamentoResponse> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int Pagina { get; init; }
    public int TamanhoPagina { get; init; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalItems / TamanhoPagina);
    public bool TemProximaPagina => Pagina < TotalPaginas;
    public bool TemPaginaAnterior => Pagina > DomainConstants.Paginacao.PaginaMinima;
}