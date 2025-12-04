using Cashflow.Abstractions;
using Cashflow.Infrastructure.Cache;
using Cashflow.Infrastructure.Configuration;

using Microsoft.Extensions.Logging;

using static Cashflow.Infrastructure.InfrastructureConstants;

namespace Cashflow.Infrastructure.Repositories;

/// <summary>
/// Decorator que adiciona cache ao repositório de saldos consolidados
/// </summary>
public class CachedSaldoConsolidadoRepository : ISaldoConsolidadoRepository
{
    private readonly ISaldoConsolidadoRepository _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedSaldoConsolidadoRepository> _logger;

    private TimeSpan CacheTtl => TimeSpan.FromMinutes(InfrastructureSettings.Cache.SaldoConsolidadoTtlMinutes);

    public CachedSaldoConsolidadoRepository(
        ISaldoConsolidadoRepository inner,
        ICacheService cache,
        ILogger<CachedSaldoConsolidadoRepository> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SaldoDiario?> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.SaldoConsolidado(data);

        return await _cache.ObterOuDefinirAsync(
            cacheKey,
            async () => await _inner.ObterPorDataAsync(data, cancellationToken),
            CacheTtl,
            cancellationToken);
    }

    public async Task<IEnumerable<SaldoDiario>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        // Para consultas de período, busca diretamente do banco
        // Poderia implementar cache por data individual, mas aumenta complexidade
        return await _inner.ObterPorPeriodoAsync(dataInicio, dataFim, cancellationToken);
    }

    public async Task SalvarAsync(SaldoDiario saldoDiario, CancellationToken cancellationToken = default)
    {
        // Salva no banco
        await _inner.SalvarAsync(saldoDiario, cancellationToken);

        // Atualiza o cache
        var cacheKey = CacheKeys.SaldoConsolidado(saldoDiario.Data);
        await _cache.DefinirAsync(cacheKey, saldoDiario, CacheTtl, cancellationToken);

        _logger.LogDebug(LogTemplates.CacheAtualizado, saldoDiario.Data.ToShortDateString());
    }

    public async Task<SaldoDiario> RecalcularAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        // Invalida o cache antes de recalcular
        var cacheKey = CacheKeys.SaldoConsolidado(data);
        await _cache.RemoverAsync(cacheKey, cancellationToken);

        // Recalcula e salva
        var saldo = await _inner.RecalcularAsync(data, cancellationToken);

        // Atualiza o cache com o novo valor
        await _cache.DefinirAsync(cacheKey, saldo, CacheTtl, cancellationToken);

        _logger.LogInformation(LogTemplates.SaldoRecalculadoCacheAtualizado, data.ToShortDateString());

        return saldo;
    }
}