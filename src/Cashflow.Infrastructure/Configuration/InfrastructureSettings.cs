namespace Cashflow.Infrastructure.Configuration;

/// <summary>
/// Configurações centralizadas da infraestrutura
/// </summary>
public static class InfrastructureSettings
{
    /// <summary>
    /// Configurações de resiliência (Polly)
    /// </summary>
    public static class Resilience
    {
        /// <summary>
        /// Número máximo de tentativas de retry
        /// </summary>
        public static int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay base para retry em milissegundos
        /// </summary>
        public static int RetryBaseDelayMs { get; set; } = 200;

        /// <summary>
        /// Delay para retry do publisher em milissegundos
        /// </summary>
        public static int PublisherRetryDelayMs { get; set; } = 500;

        /// <summary>
        /// Duração do circuit breaker em segundos
        /// </summary>
        public static int CircuitBreakerDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Proporção de falhas para abrir o circuit breaker (0.0 a 1.0)
        /// </summary>
        public static double CircuitBreakerFailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Throughput mínimo para avaliar o circuit breaker (cache)
        /// </summary>
        public static int CacheMinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Throughput mínimo para avaliar o circuit breaker (messaging)
        /// </summary>
        public static int MessagingMinimumThroughput { get; set; } = 5;

        /// <summary>
        /// Janela de amostragem do circuit breaker em segundos
        /// </summary>
        public static int SamplingDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Timeout para operações de cache em segundos
        /// </summary>
        public static int CacheTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Timeout para operações de mensageria em segundos
        /// </summary>
        public static int MessagingTimeoutSeconds { get; set; } = 10;
    }

    /// <summary>
    /// Configurações de cache
    /// </summary>
    public static class Cache
    {
        /// <summary>
        /// TTL padrão do cache em minutos
        /// </summary>
        public static int DefaultTtlMinutes { get; set; } = 30;

        /// <summary>
        /// TTL do cache de saldo consolidado em minutos
        /// </summary>
        public static int SaldoConsolidadoTtlMinutes { get; set; } = 15;

        /// <summary>
        /// Prefixo para chaves de cache no Redis
        /// </summary>
        public static string InstanceName { get; set; } = "Cashflow:";
    }

    /// <summary>
    /// Configurações do banco de dados
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Número máximo de tentativas de retry
        /// </summary>
        public static int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Delay máximo entre retries em segundos
        /// </summary>
        public static int MaxRetryDelaySeconds { get; set; } = 30;

        /// <summary>
        /// Timeout de comando em segundos
        /// </summary>
        public static int CommandTimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Carrega configurações das variáveis de ambiente
    /// </summary>
    public static void LoadFromEnvironment()
    {
        // Resilience
        Resilience.MaxRetryAttempts = GetEnvInt("RESILIENCE_MAX_RETRY_ATTEMPTS", Resilience.MaxRetryAttempts);
        Resilience.RetryBaseDelayMs = GetEnvInt("RESILIENCE_RETRY_BASE_DELAY_MS", Resilience.RetryBaseDelayMs);
        Resilience.PublisherRetryDelayMs = GetEnvInt("RESILIENCE_PUBLISHER_RETRY_DELAY_MS", Resilience.PublisherRetryDelayMs);
        Resilience.CircuitBreakerDurationSeconds = GetEnvInt("RESILIENCE_CIRCUIT_BREAKER_DURATION_SECONDS", Resilience.CircuitBreakerDurationSeconds);
        Resilience.CircuitBreakerFailureRatio = GetEnvDouble("RESILIENCE_CIRCUIT_BREAKER_FAILURE_RATIO", Resilience.CircuitBreakerFailureRatio);
        Resilience.CacheMinimumThroughput = GetEnvInt("RESILIENCE_CACHE_MINIMUM_THROUGHPUT", Resilience.CacheMinimumThroughput);
        Resilience.MessagingMinimumThroughput = GetEnvInt("RESILIENCE_MESSAGING_MINIMUM_THROUGHPUT", Resilience.MessagingMinimumThroughput);
        Resilience.SamplingDurationSeconds = GetEnvInt("RESILIENCE_SAMPLING_DURATION_SECONDS", Resilience.SamplingDurationSeconds);
        Resilience.CacheTimeoutSeconds = GetEnvInt("RESILIENCE_CACHE_TIMEOUT_SECONDS", Resilience.CacheTimeoutSeconds);
        Resilience.MessagingTimeoutSeconds = GetEnvInt("RESILIENCE_MESSAGING_TIMEOUT_SECONDS", Resilience.MessagingTimeoutSeconds);

        // Cache
        Cache.DefaultTtlMinutes = GetEnvInt("CACHE_DEFAULT_TTL_MINUTES", Cache.DefaultTtlMinutes);
        Cache.SaldoConsolidadoTtlMinutes = GetEnvInt("CACHE_SALDO_CONSOLIDADO_TTL_MINUTES", Cache.SaldoConsolidadoTtlMinutes);
        Cache.InstanceName = GetEnvString("CACHE_INSTANCE_NAME", Cache.InstanceName);

        // Database
        Database.MaxRetryCount = GetEnvInt("DATABASE_MAX_RETRY_COUNT", Database.MaxRetryCount);
        Database.MaxRetryDelaySeconds = GetEnvInt("DATABASE_MAX_RETRY_DELAY_SECONDS", Database.MaxRetryDelaySeconds);
        Database.CommandTimeoutSeconds = GetEnvInt("DATABASE_COMMAND_TIMEOUT_SECONDS", Database.CommandTimeoutSeconds);
    }

    private static int GetEnvInt(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static double GetEnvDouble(string key, double defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return double.TryParse(value, out var result) ? result : defaultValue;
    }

    private static string GetEnvString(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
}


