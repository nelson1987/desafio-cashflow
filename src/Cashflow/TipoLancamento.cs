namespace Cashflow;

/// <summary>
/// Tipo de lançamento no fluxo de caixa
/// </summary>
public enum TipoLancamento
{
    /// <summary>
    /// Entrada de dinheiro no caixa (aumenta o saldo)
    /// </summary>
    Credito = 1,

    /// <summary>
    /// Saída de dinheiro do caixa (diminui o saldo)
    /// </summary>
    Debito = 2
}