namespace Cashflow.Infrastructure.Data;

/// <summary>
/// Configurações do banco de dados
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>
    /// Se deve aplicar migrations automaticamente na inicialização
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = false;

    /// <summary>
    /// Se deve fazer seed de dados iniciais
    /// </summary>
    public bool SeedDataOnStartup { get; set; } = false;

    /// <summary>
    /// Timeout de conexão em segundos
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Número máximo de tentativas de retry
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Delay máximo entre retries em segundos
    /// </summary>
    public int MaxRetryDelay { get; set; } = 30;
}
