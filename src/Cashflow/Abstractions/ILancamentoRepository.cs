namespace Cashflow.Abstractions;

/// <summary>
/// Interface do repositório de lançamentos
/// </summary>
public interface ILancamentoRepository
{
    /// <summary>
    /// Obtém um lançamento por ID
    /// </summary>
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os lançamentos de um dia específico
    /// </summary>
    Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém lançamentos em um período
    /// </summary>
    Task<IEnumerable<Lancamento>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo lançamento
    /// </summary>
    Task<Lancamento> AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os lançamentos com paginação
    /// </summary>
    Task<IEnumerable<Lancamento>> ObterTodosAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta o total de lançamentos
    /// </summary>
    Task<int> ContarAsync(CancellationToken cancellationToken = default);
}
