namespace Cashflow;

/// <summary>
/// Representa o saldo consolidado de um dia específico
/// </summary>
public class SaldoDiario
{
    public DateTime Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public int QuantidadeLancamentos { get; private set; }

    /// <summary>
    /// Saldo do dia (créditos - débitos)
    /// </summary>
    public decimal Saldo => TotalCreditos - TotalDebitos;

    private SaldoDiario() { }

    /// <summary>
    /// Construtor para criação direta com valores (usado em testes e reconstrução de persistência)
    /// </summary>
    public SaldoDiario(DateTime data, decimal totalCreditos, decimal totalDebitos, int quantidadeLancamentos)
    {
        Data = data.Date;
        TotalCreditos = totalCreditos;
        TotalDebitos = totalDebitos;
        QuantidadeLancamentos = quantidadeLancamentos;
    }

    public SaldoDiario(DateTime data, IEnumerable<Lancamento> lancamentos)
    {
        ArgumentNullException.ThrowIfNull(lancamentos);

        Data = data.Date;
        var lancamentosDoDia = lancamentos.Where(l => l.EhDoDia(data)).ToList();

        TotalCreditos = lancamentosDoDia
            .Where(l => l.Tipo == TipoLancamento.Credito)
            .Sum(l => l.Valor);

        TotalDebitos = lancamentosDoDia
            .Where(l => l.Tipo == TipoLancamento.Debito)
            .Sum(l => l.Valor);

        QuantidadeLancamentos = lancamentosDoDia.Count;
    }

    /// <summary>
    /// Cria um saldo diário vazio para o dia informado
    /// </summary>
    public static SaldoDiario Vazio(DateTime data) => new()
    {
        Data = data.Date,
        TotalCreditos = DomainConstants.ValoresPadrao.Zero,
        TotalDebitos = DomainConstants.ValoresPadrao.Zero,
        QuantidadeLancamentos = DomainConstants.ValoresPadrao.QuantidadeZero
    };
}