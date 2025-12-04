namespace Cashflow.Events;

/// <summary>
/// Evento publicado quando um novo lançamento é criado
/// </summary>
public record LancamentoCriadoEvent
{
    /// <summary>
    /// ID do lançamento criado
    /// </summary>
    public Guid LancamentoId { get; init; }

    /// <summary>
    /// Data do lançamento (usada para recalcular o consolidado)
    /// </summary>
    public DateTime Data { get; init; }

    /// <summary>
    /// Tipo do lançamento (Crédito ou Débito)
    /// </summary>
    public TipoLancamento Tipo { get; init; }

    /// <summary>
    /// Valor do lançamento
    /// </summary>
    public decimal Valor { get; init; }

    /// <summary>
    /// Timestamp de quando o evento foi criado
    /// </summary>
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;

    public LancamentoCriadoEvent() { }

    public LancamentoCriadoEvent(Lancamento lancamento)
    {
        LancamentoId = lancamento.Id;
        Data = lancamento.Data;
        Tipo = lancamento.Tipo;
        Valor = lancamento.Valor;
    }
}