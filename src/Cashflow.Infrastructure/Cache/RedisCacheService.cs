using System.Text.Json;
using Cashflow.Abstractions;
using Cashflow.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Cashflow.Infrastructure.Cache;

/// <summary>
/// Implementação do serviço de cache usando Redis com resiliência (Polly)
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
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
                        "Tentativa {Attempt} de acesso ao cache falhou. Tentando novamente em {Delay}ms",
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
                        "Circuit breaker ABERTO para o cache. Duração: {Duration}s",
                        circuitBreakerDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker FECHADO para o cache");
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
            _logger.LogError(ex, "Erro ao obter valor do cache. Chave: {Chave}", chave);
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
            _logger.LogError(ex, "Erro ao definir valor no cache. Chave: {Chave}", chave);
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
            _logger.LogError(ex, "Erro ao remover valor do cache. Chave: {Chave}", chave);
        }
    }

    public async Task RemoverPorPrefixoAsync(string prefixo, CancellationToken cancellationToken = default)
    {
        // Nota: IDistributedCache não suporta remoção por prefixo nativamente
        // Para implementação real, seria necessário usar StackExchange.Redis diretamente
        // com SCAN e DEL ou usar um padrão de invalidação diferente
        _logger.LogWarning(
            "RemoverPorPrefixoAsync não é suportado nativamente por IDistributedCache. Prefixo: {Prefixo}",
            prefixo);
        await Task.CompletedTask;
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
            _logger.LogError(ex, "Erro ao verificar existência no cache. Chave: {Chave}", chave);
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
