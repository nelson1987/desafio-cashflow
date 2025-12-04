namespace Cashflow.Infrastructure.Data.Entities;

/// <summary>
/// Entidade de persistência para Saldo Consolidado
/// </summary>
public class SaldoConsolidadoEntity
{
    public DateTime Data { get; set; }
    public decimal TotalCreditos { get; set; }
    public decimal TotalDebitos { get; set; }
    public decimal Saldo { get; set; }
    public int QuantidadeLancamentos { get; set; }
    public DateTime ProcessadoEm { get; set; }

    /// <summary>
    /// Converte a entidade de persistência para o modelo de domínio
    /// </summary>
    public SaldoDiario ToDomain()
    {
        // Usa reflection para criar o SaldoDiario sem passar pelo construtor
        var saldo = (SaldoDiario)Activator.CreateInstance(typeof(SaldoDiario), nonPublic: true)!;
        
        var dataProperty = typeof(SaldoDiario).GetProperty(nameof(SaldoDiario.Data));
        var totalCreditosProperty = typeof(SaldoDiario).GetProperty(nameof(SaldoDiario.TotalCreditos));
        var totalDebitosProperty = typeof(SaldoDiario).GetProperty(nameof(SaldoDiario.TotalDebitos));
        var quantidadeProperty = typeof(SaldoDiario).GetProperty(nameof(SaldoDiario.QuantidadeLancamentos));

        dataProperty?.SetValue(saldo, Data);
        totalCreditosProperty?.SetValue(saldo, TotalCreditos);
        totalDebitosProperty?.SetValue(saldo, TotalDebitos);
        quantidadeProperty?.SetValue(saldo, QuantidadeLancamentos);

        return saldo;
    }

    /// <summary>
    /// Cria uma entidade de persistência a partir do modelo de domínio
    /// </summary>
    public static SaldoConsolidadoEntity FromDomain(SaldoDiario saldoDiario)
    {
        return new SaldoConsolidadoEntity
        {
            Data = saldoDiario.Data.Date,
            TotalCreditos = saldoDiario.TotalCreditos,
            TotalDebitos = saldoDiario.TotalDebitos,
            Saldo = saldoDiario.Saldo,
            QuantidadeLancamentos = saldoDiario.QuantidadeLancamentos,
            ProcessadoEm = DateTime.UtcNow
        };
    }
}

