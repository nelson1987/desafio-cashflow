namespace Cashflow;

/// <summary>
/// Agregado raiz que representa o fluxo de caixa do comerciante
/// </summary>
public class FluxoCaixa
{
    private readonly List<Lancamento> _lancamentos = [];

    public IReadOnlyCollection<Lancamento> Lancamentos => _lancamentos.AsReadOnly();

    /// <summary>
    /// Registra um novo lançamento de crédito (entrada)
    /// </summary>
    public Lancamento RegistrarCredito(decimal valor, DateTime data, string descricao)
    {
        var lancamento = new Lancamento(valor, TipoLancamento.Credito, data, descricao);
        _lancamentos.Add(lancamento);
        return lancamento;
    }

    /// <summary>
    /// Registra um novo lançamento de débito (saída)
    /// </summary>
    public Lancamento RegistrarDebito(decimal valor, DateTime data, string descricao)
    {
        var lancamento = new Lancamento(valor, TipoLancamento.Debito, data, descricao);
        _lancamentos.Add(lancamento);
        return lancamento;
    }

    /// <summary>
    /// Obtém o saldo consolidado de um dia específico
    /// </summary>
    public SaldoDiario ObterSaldoDiario(DateTime data)
    {
        return new SaldoDiario(data, _lancamentos);
    }

    /// <summary>
    /// Obtém o relatório de saldos diários consolidados para um período
    /// </summary>
    public IEnumerable<SaldoDiario> ObterRelatorioConsolidado(DateTime dataInicio, DateTime dataFim)
    {
        if (dataInicio > dataFim)
            throw new ArgumentException("A data de início deve ser menor ou igual à data de fim.");

        var resultado = new List<SaldoDiario>();
        var dataAtual = dataInicio.Date;

        while (dataAtual <= dataFim.Date)
        {
            resultado.Add(ObterSaldoDiario(dataAtual));
            dataAtual = dataAtual.AddDays(1);
        }

        return resultado;
    }

    /// <summary>
    /// Obtém o saldo acumulado até uma data específica
    /// </summary>
    public decimal ObterSaldoAcumulado(DateTime data)
    {
        return _lancamentos
            .Where(l => l.Data <= data.Date)
            .Sum(l => l.ValorComSinal);
    }

    /// <summary>
    /// Obtém todos os lançamentos de um dia específico
    /// </summary>
    public IEnumerable<Lancamento> ObterLancamentosDoDia(DateTime data)
    {
        return _lancamentos.Where(l => l.EhDoDia(data));
    }
}