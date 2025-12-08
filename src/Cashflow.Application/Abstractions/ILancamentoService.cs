using Cashflow.Application.DTOs;

namespace Cashflow.Application.Abstractions;

/// <summary>
/// Interface do serviço de lançamentos
/// </summary>
public interface ILancamentoService
{
    /// <summary>
    /// Cria um novo lançamento
    /// </summary>
    Task<Result<LancamentoResponse>> CriarAsync(CriarLancamentoRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um lançamento por ID
    /// </summary>
    Task<Result<LancamentoResponse>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista lançamentos com paginação
    /// </summary>
    Task<Result<LancamentosListResponse>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém lançamentos de uma data específica
    /// </summary>
    Task<Result<IEnumerable<LancamentoResponse>>> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default);
}
