namespace Cashflow.Abstractions;

/// <summary>
/// Interface do repositório de saldos consolidados
/// </summary>
public interface ISaldoConsolidadoRepository
{
    /// <summary>
    /// Obtém o saldo consolidado de um dia específico
    /// </summary>
    Task<SaldoDiario?> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém saldos consolidados de um período
    /// </summary>
    Task<IEnumerable<SaldoDiario>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);

    /// <summary>
    /// Salva ou atualiza o saldo consolidado de um dia
    /// </summary>
    Task SalvarAsync(SaldoDiario saldoDiario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalcula o saldo consolidado de um dia a partir dos lançamentos
    /// </summary>
    Task<SaldoDiario> RecalcularAsync(DateTime data, CancellationToken cancellationToken = default);
}
