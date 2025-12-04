using Cashflow.Abstractions;
using Cashflow.Infrastructure.Cache;
using Cashflow.Infrastructure.Configuration;
using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Messaging;
using Cashflow.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using RabbitMQ.Client;

namespace Cashflow.Infrastructure;

/// <summary>
/// Extensões para configuração da camada de infraestrutura
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona os serviços de infraestrutura ao container de DI
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    /// <param name="loadEnvFile">Se deve carregar o arquivo .env</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool loadEnvFile = true)
    {
        // Carrega variáveis de ambiente do arquivo .env
        if (loadEnvFile)
        {
            EnvironmentLoader.Load();
        }

        // Configura o DbContext com PostgreSQL
        services.AddDbContext<CashflowDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_POSTGRESQL")
                ?? throw new InvalidOperationException("Connection string 'PostgreSQL' não encontrada.");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: InfrastructureSettings.Database.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(InfrastructureSettings.Database.MaxRetryDelaySeconds),
                    errorCodesToAdd: null);

                npgsqlOptions.CommandTimeout(InfrastructureSettings.Database.CommandTimeoutSeconds);
            });
        });

        // Registra os repositórios
        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<SaldoConsolidadoRepository>();
        services.AddScoped<ISaldoConsolidadoRepository>(sp =>
        {
            var innerRepository = sp.GetRequiredService<SaldoConsolidadoRepository>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedSaldoConsolidadoRepository>>();
            return new CachedSaldoConsolidadoRepository(innerRepository, cacheService, logger);
        });

        // Configura o Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_REDIS")
                ?? "localhost:6379";
            options.InstanceName = InfrastructureSettings.Cache.InstanceName;
        });

        // Registra o serviço de cache
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Configura o RabbitMQ
        services.Configure<RabbitMqSettings>(opts =>
        {
            var section = configuration.GetSection(RabbitMqSettings.SectionName);

            opts.Host = section["Host"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                ?? "localhost";
            opts.Port = int.TryParse(section["Port"] ?? Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port)
                ? port : 5672;
            opts.Username = section["Username"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ_USER")
                ?? "guest";
            opts.Password = section["Password"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                ?? "guest";
            opts.VirtualHost = section["VirtualHost"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ_VHOST")
                ?? "/";
            opts.Exchange = section["Exchange"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE")
                ?? "cashflow.events";
            opts.ExchangeType = section["ExchangeType"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE_TYPE")
                ?? "topic";
            opts.AutomaticRecoveryEnabled = bool.TryParse(
                section["AutomaticRecoveryEnabled"] ?? Environment.GetEnvironmentVariable("RABBITMQ_AUTOMATIC_RECOVERY"),
                out var autoRecovery) ? autoRecovery : true;
            opts.NetworkRecoveryInterval = int.TryParse(
                section["NetworkRecoveryInterval"] ?? Environment.GetEnvironmentVariable("RABBITMQ_NETWORK_RECOVERY_INTERVAL"),
                out var interval) ? interval : 10;
        });

        // Registra o publicador de mensagens
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return services;
    }

    /// <summary>
    /// Adiciona os health checks para os serviços de infraestrutura
    /// </summary>
    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        // Health check do PostgreSQL
        var postgresConnection = configuration.GetConnectionString("PostgreSQL")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_POSTGRESQL");

        if (!string.IsNullOrEmpty(postgresConnection))
        {
            healthChecks.AddNpgSql(
                connectionString: postgresConnection,
                name: "postgresql",
                tags: ["db", "sql", "postgresql"]);
        }

        // Health check do Redis
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_REDIS");

        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecks.AddRedis(
                redisConnectionString: redisConnection,
                name: "redis",
                tags: ["cache", "redis"]);
        }

        // Health check do RabbitMQ
        var rabbitHost = configuration.GetSection("RabbitMQ:Host").Value
            ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST");

        if (!string.IsNullOrEmpty(rabbitHost))
        {
            healthChecks.AddRabbitMQ(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitHost,
                    Port = int.TryParse(
                        configuration.GetSection("RabbitMQ:Port").Value
                        ?? Environment.GetEnvironmentVariable("RABBITMQ_PORT"),
                        out var port) ? port : 5672,
                    UserName = configuration.GetSection("RabbitMQ:Username").Value
                        ?? Environment.GetEnvironmentVariable("RABBITMQ_USER")
                        ?? "guest",
                    Password = configuration.GetSection("RabbitMQ:Password").Value
                        ?? Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                        ?? "guest",
                    VirtualHost = configuration.GetSection("RabbitMQ:VirtualHost").Value
                        ?? Environment.GetEnvironmentVariable("RABBITMQ_VHOST")
                        ?? "/"
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            },
            name: "rabbitmq",
            tags: ["messaging", "rabbitmq"]);
        }

        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes do banco de dados
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CashflowDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Verifica se o banco de dados está acessível
    /// </summary>
    public static async Task<bool> CanConnectToDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CashflowDbContext>();
        return await context.Database.CanConnectAsync();
    }
}