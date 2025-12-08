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
    /// Tipo do lançamento (Crédito ou Débito) - serializado como string para compatibilidade
    /// </summary>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>
    /// Valor do lançamento
    /// </summary>
    public decimal Valor { get; init; }

    /// <summary>
    /// Descrição do lançamento
    /// </summary>
    public string Descricao { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp de quando o evento foi criado
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public LancamentoCriadoEvent() { }

    public LancamentoCriadoEvent(Lancamento lancamento)
    {
        LancamentoId = lancamento.Id;
        Data = lancamento.Data;
        Tipo = lancamento.Tipo.ToString();
        Valor = lancamento.Valor;
        Descricao = lancamento.Descricao;
    }
}
