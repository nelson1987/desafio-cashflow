using System.Text.Json;

using Cashflow.Abstractions;
using Cashflow.Infrastructure.Configuration;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

using StackExchange.Redis;

using static Cashflow.Infrastructure.InfrastructureConstants;

namespace Cashflow.Infrastructure.Cache;

/// <summary>
/// Implementação do serviço de cache usando Redis com resiliência (Polly)
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _instanceName;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _cache = cache;
        _redisConnection = redisConnection;
        _logger = logger;
        _instanceName = InfrastructureSettings.Cache.InstanceName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var circuitBreakerDuration = TimeSpan.FromSeconds(InfrastructureSettings.Resilience.CircuitBreakerDurationSeconds);

        // Configura pipeline de resiliência com Polly
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = InfrastructureSettings.Resilience.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(InfrastructureSettings.Resilience.RetryBaseDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        LogTemplates.RetryCache,
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = InfrastructureSettings.Resilience.CircuitBreakerFailureRatio,
                MinimumThroughput = InfrastructureSettings.Resilience.CacheMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(InfrastructureSettings.Resilience.SamplingDurationSeconds),
                BreakDuration = circuitBreakerDuration,
                OnOpened = args =>
                {
                    _logger.LogWarning(
                        LogTemplates.CircuitBreakerAbertoCache,
                        circuitBreakerDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(LogTemplates.CircuitBreakerFechadoCache);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(InfrastructureSettings.Resilience.CacheTimeoutSeconds))
            .Build();
    }

    private TimeSpan DefaultTtl => TimeSpan.FromMinutes(InfrastructureSettings.Cache.DefaultTtlMinutes);

    public async Task<T?> ObterAsync<T>(string chave, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var bytes = await _cache.GetAsync(chave, ct);

                if (bytes == null || bytes.Length == 0)
                    return null;

                return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroObterCache, chave);
            return null; // Fail gracefully - retorna null em caso de erro
        }
    }

    public async Task DefinirAsync<T>(string chave, T valor, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(valor, _jsonOptions);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
                };

                await _cache.SetAsync(chave, bytes, options, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroDefinirCache, chave);
            // Fail gracefully - não propaga erro para não impactar o fluxo principal
        }
    }

    public async Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                await _cache.RemoveAsync(chave, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroRemoverCache, chave);
        }
    }

    public async Task RemoverPorPrefixoAsync(string prefixo, CancellationToken cancellationToken = default)
    {
        if (_redisConnection == null)
        {
            _logger.LogWarning(
                "RemoverPorPrefixoAsync: IConnectionMultiplexer não disponível, operação ignorada para prefixo: {Prefixo}",
                prefixo);
            return;
        }

        try
        {
            await _resiliencePipeline.ExecuteAsync(async _ =>
            {
                var database = _redisConnection.GetDatabase();
                var server = _redisConnection.GetServers().FirstOrDefault();

                if (server == null)
                {
                    _logger.LogWarning("RemoverPorPrefixoAsync: Nenhum servidor Redis disponível");
                    return;
                }

                // Adiciona o prefixo da instância se configurado
                var pattern = $"{_instanceName}{prefixo}*";

                var keysToDelete = new List<RedisKey>();

                // Usa SCAN para encontrar chaves de forma eficiente (não bloqueante)
                await foreach (var key in server.KeysAsync(pattern: pattern))
                {
                    keysToDelete.Add(key);
                }

                if (keysToDelete.Count > 0)
                {
                    await database.KeyDeleteAsync(keysToDelete.ToArray());
                    _logger.LogInformation(
                        "RemoverPorPrefixoAsync: {Count} chaves removidas com prefixo: {Prefixo}",
                        keysToDelete.Count,
                        prefixo);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover chaves do cache por prefixo: {Prefixo}", prefixo);
        }
    }

    public async Task<bool> ExisteAsync(string chave, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var bytes = await _cache.GetAsync(chave, ct);
                return bytes != null && bytes.Length > 0;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroVerificarExistenciaCache, chave);
            return false;
        }
    }

    public async Task<T?> ObterOuDefinirAsync<T>(
        string chave,
        Func<Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Tenta obter do cache primeiro
        var cached = await ObterAsync<T>(chave, cancellationToken);
        if (cached != null)
            return cached;

        // Se não encontrou, executa a factory
        var valor = await factory();

        if (valor != null)
        {
            await DefinirAsync(chave, valor, ttl, cancellationToken);
        }

        return valor;
    }
}

/// <summary>
/// Chaves de cache do sistema
/// </summary>
public static class CacheKeys
{
    public const string SaldoConsolidadoPrefix = "saldo:consolidado:";

    public static string SaldoConsolidado(DateTime data) => $"{SaldoConsolidadoPrefix}{data:yyyy-MM-dd}";

    public static string Lancamento(Guid id) => $"lancamento:{id}";

    public static string LancamentosDoDia(DateTime data) => $"lancamentos:dia:{data:yyyy-MM-dd}";
}