using static Cashflow.DomainConstants;

namespace Cashflow.Application.DTOs;

/// <summary>
/// DTO de resposta do saldo consolidado diário
/// </summary>
public record SaldoConsolidadoResponse
{
    public DateTime Data { get; init; }
    public decimal TotalCreditos { get; init; }
    public decimal TotalDebitos { get; init; }
    public decimal Saldo { get; init; }
    public int QuantidadeLancamentos { get; init; }

    public static SaldoConsolidadoResponse FromDomain(SaldoDiario saldo) => new()
    {
        Data = saldo.Data,
        TotalCreditos = saldo.TotalCreditos,
        TotalDebitos = saldo.TotalDebitos,
        Saldo = saldo.Saldo,
        QuantidadeLancamentos = saldo.QuantidadeLancamentos
    };

    public static SaldoConsolidadoResponse Vazio(DateTime data) => new()
    {
        Data = data.Date,
        TotalCreditos = ValoresPadrao.Zero,
        TotalDebitos = ValoresPadrao.Zero,
        Saldo = ValoresPadrao.Zero,
        QuantidadeLancamentos = ValoresPadrao.QuantidadeZero
    };
}

/// <summary>
/// DTO para relatório de saldos consolidados por período
/// </summary>
public record RelatorioConsolidadoResponse
{
    public DateTime DataInicio { get; init; }
    public DateTime DataFim { get; init; }
    public IEnumerable<SaldoConsolidadoResponse> Saldos { get; init; } = [];
    public ResumoConsolidadoResponse Resumo { get; init; } = new();
}

/// <summary>
/// DTO de resumo do período consolidado
/// </summary>
public record ResumoConsolidadoResponse
{
    public decimal TotalCreditos { get; init; }
    public decimal TotalDebitos { get; init; }
    public decimal SaldoFinal { get; init; }
    public int TotalLancamentos { get; init; }
    public int DiasComMovimentacao { get; init; }
}