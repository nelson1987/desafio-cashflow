using Cashflow.Abstractions;
using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;

using Microsoft.Extensions.Logging;

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
                _logger.LogDebug("Saldo consolidado não encontrado para data: {Data}. Retornando saldo vazio.", data);
                return Result.Success(SaldoConsolidadoResponse.Vazio(data));
            }

            return Result.Success(SaldoConsolidadoResponse.FromDomain(saldo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter saldo consolidado para data: {Data}", data);
            return Result.Failure<SaldoConsolidadoResponse>("Ocorreu um erro ao buscar o saldo consolidado.");
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
            return Result.Failure<RelatorioConsolidadoResponse>("A data inicial não pode ser maior que a data final.");
        }

        // Limita período máximo de 90 dias
        if ((dataFim - dataInicio).Days > 90)
        {
            return Result.Failure<RelatorioConsolidadoResponse>("O período máximo permitido é de 90 dias.");
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
            _logger.LogError(ex, "Erro ao obter relatório consolidado. Período: {DataInicio} a {DataFim}", dataInicio, dataFim);
            return Result.Failure<RelatorioConsolidadoResponse>("Ocorreu um erro ao gerar o relatório consolidado.");
        }
    }

    public async Task<Result<SaldoConsolidadoResponse>> RecalcularAsync(
        DateTime data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando recálculo do saldo consolidado para data: {Data}", data);

            var saldo = await _repository.RecalcularAsync(data, cancellationToken);

            _logger.LogInformation(
                "Saldo consolidado recalculado. Data: {Data}, Saldo: {Saldo}, Lançamentos: {Qtd}",
                data,
                saldo.Saldo,
                saldo.QuantidadeLancamentos);

            return Result.Success(SaldoConsolidadoResponse.FromDomain(saldo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recalcular saldo consolidado para data: {Data}", data);
            return Result.Failure<SaldoConsolidadoResponse>("Ocorreu um erro ao recalcular o saldo consolidado.");
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

        for (var data = dataInicio.Date; data <= dataFim.Date; data = data.AddDays(1))
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
            DiasComMovimentacao = saldos.Count(s => s.QuantidadeLancamentos > 0)
        };
    }

    #endregion
}

