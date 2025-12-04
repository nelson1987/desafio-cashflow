namespace Cashflow.Abstractions;

/// <summary>
/// Interface para serviço de cache distribuído
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém um valor do cache
    /// </summary>
    Task<T?> ObterAsync<T>(string chave, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Define um valor no cache com TTL opcional
    /// </summary>
    Task DefinirAsync<T>(string chave, T valor, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove um valor do cache
    /// </summary>
    Task RemoverAsync(string chave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove valores do cache por prefixo de chave
    /// </summary>
    Task RemoverPorPrefixoAsync(string prefixo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se uma chave existe no cache
    /// </summary>
    Task<bool> ExisteAsync(string chave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém ou define um valor no cache (cache-aside pattern)
    /// </summary>
    Task<T?> ObterOuDefinirAsync<T>(string chave, Func<Task<T?>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class;
}


