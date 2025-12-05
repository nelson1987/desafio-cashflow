using Cashflow.Application.DTOs;

namespace Cashflow.Application.Abstractions;

/// <summary>
/// Interface do serviço de consolidado diário
/// </summary>
public interface IConsolidadoService
{
    /// <summary>
    /// Obtém o saldo consolidado de uma data específica
    /// </summary>
    Task<Result<SaldoConsolidadoResponse>> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o relatório consolidado de um período
    /// </summary>
    Task<Result<RelatorioConsolidadoResponse>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalcula o saldo consolidado de uma data (usado pelo worker)
    /// </summary>
    Task<Result<SaldoConsolidadoResponse>> RecalcularAsync(DateTime data, CancellationToken cancellationToken = default);
}