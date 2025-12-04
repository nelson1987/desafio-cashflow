using Cashflow.Abstractions;
using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;

using Microsoft.Extensions.Logging;

using static Cashflow.Application.ApplicationConstants;
using static Cashflow.DomainConstants;

namespace Cashflow.Application.Services;

/// <summary>
/// Serviço de consolidado diário (Use Case)
/// </summary>
public class ConsolidadoService : IConsolidadoService
{
    private readonly ISaldoConsolidadoRepository _repository;
    private readonly ILogger<ConsolidadoService> _logger;

    public ConsolidadoService(
        ISaldoConsolidadoRepository repository,
        ILogger<ConsolidadoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<SaldoConsolidadoResponse>> ObterPorDataAsync(
        DateTime data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var saldo = await _repository.ObterPorDataAsync(data, cancellationToken);

            if (saldo is null)
            {
                _logger.LogDebug(LogTemplates.SaldoNaoEncontrado, data);
                return Result.Success(SaldoConsolidadoResponse.Vazio(data));
            }

            return Result.Success(SaldoConsolidadoResponse.FromDomain(saldo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroObterSaldoPorData, data);
            return Result.Failure<SaldoConsolidadoResponse>(ErrosConsolidado.ErroAoBuscar);
        }
    }

    public async Task<Result<RelatorioConsolidadoResponse>> ObterPorPeriodoAsync(
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken cancellationToken = default)
    {
        // Validação de período
        if (dataInicio > dataFim)
        {
            return Result.Failure<RelatorioConsolidadoResponse>(ErrosConsolidado.DataInicialMaiorQueFinal);
        }

        // Limita período máximo
        if ((dataFim - dataInicio).Days > Consolidacao.PeriodoMaximoDias)
        {
            return Result.Failure<RelatorioConsolidadoResponse>(
                string.Format(ErrosConsolidado.PeriodoMaximoExcedido, Consolidacao.PeriodoMaximoDias));
        }

        try
        {
            var saldos = await _repository.ObterPorPeriodoAsync(dataInicio, dataFim, cancellationToken);
            var saldosList = saldos.ToList();

            // Preenche dias sem movimentação
            var saldosCompletos = PreencherDiasSemMovimentacao(dataInicio, dataFim, saldosList);

            // Calcula resumo
            var resumo = CalcularResumo(saldosList);

            var response = new RelatorioConsolidadoResponse
            {
                DataInicio = dataInicio.Date,
                DataFim = dataFim.Date,
                Saldos = saldosCompletos,
                Resumo = resumo
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroObterRelatorio, dataInicio, dataFim);
            return Result.Failure<RelatorioConsolidadoResponse>(ErrosConsolidado.ErroAoGerarRelatorio);
        }
    }

    public async Task<Result<SaldoConsolidadoResponse>> RecalcularAsync(
        DateTime data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(LogTemplates.IniciandoRecalculo, data);

            var saldo = await _repository.RecalcularAsync(data, cancellationToken);

            _logger.LogInformation(
                LogTemplates.SaldoRecalculado,
                data,
                saldo.Saldo,
                saldo.QuantidadeLancamentos);

            return Result.Success(SaldoConsolidadoResponse.FromDomain(saldo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroRecalcular, data);
            return Result.Failure<SaldoConsolidadoResponse>(ErrosConsolidado.ErroAoRecalcular);
        }
    }

    #region Private Methods

    private static IEnumerable<SaldoConsolidadoResponse> PreencherDiasSemMovimentacao(
        DateTime dataInicio,
        DateTime dataFim,
        List<SaldoDiario> saldosExistentes)
    {
        var saldosPorData = saldosExistentes.ToDictionary(s => s.Data.Date);
        var resultado = new List<SaldoConsolidadoResponse>();

        for (var data = dataInicio.Date; data <= dataFim.Date; data = data.AddDays(Consolidacao.IncrementoDia))
        {
            if (saldosPorData.TryGetValue(data, out var saldo))
            {
                resultado.Add(SaldoConsolidadoResponse.FromDomain(saldo));
            }
            else
            {
                resultado.Add(SaldoConsolidadoResponse.Vazio(data));
            }
        }

        return resultado;
    }

    private static ResumoConsolidadoResponse CalcularResumo(List<SaldoDiario> saldos)
    {
        return new ResumoConsolidadoResponse
        {
            TotalCreditos = saldos.Sum(s => s.TotalCreditos),
            TotalDebitos = saldos.Sum(s => s.TotalDebitos),
            SaldoFinal = saldos.Sum(s => s.Saldo),
            TotalLancamentos = saldos.Sum(s => s.QuantidadeLancamentos),
            DiasComMovimentacao = saldos.Count(s => s.QuantidadeLancamentos > ValoresPadrao.QuantidadeZero)
        };
    }

    #endregion
}