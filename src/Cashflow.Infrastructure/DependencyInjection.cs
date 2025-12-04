using Cashflow.Abstractions;
using Cashflow.Infrastructure.Cache;
using Cashflow.Infrastructure.Configuration;
using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Messaging;
using Cashflow.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

namespace Cashflow.Infrastructure;

/// <summary>
/// Extensões para configuração da camada de infraestrutura
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona todos os serviços de infraestrutura ao container de DI
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool loadEnvFile = true)
    {
        if (loadEnvFile)
        {
            EnvironmentLoader.Load();
        }

        services
            .AddPostgreSql(configuration)
            .AddRedisCache(configuration)
            .AddRabbitMq(configuration)
            .AddRepositories();

        return services;
    }

    /// <summary>
    /// Adiciona o PostgreSQL e DbContext
    /// </summary>
    public static IServiceCollection AddPostgreSql(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CashflowDbContext>(options =>
        {
            var connectionString = GetConnectionString(configuration, "PostgreSQL", "CONNECTION_STRING_POSTGRESQL");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: InfrastructureSettings.Database.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(InfrastructureSettings.Database.MaxRetryDelaySeconds),
                    errorCodesToAdd: null);

                npgsqlOptions.CommandTimeout(InfrastructureSettings.Database.CommandTimeoutSeconds);
            });
        });

        return services;
    }

    /// <summary>
    /// Adiciona o Redis Cache
    /// </summary>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING_REDIS")
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = InfrastructureSettings.Cache.InstanceName;
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    /// <summary>
    /// Adiciona o RabbitMQ para mensageria
    /// </summary>
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(opts =>
        {
            var section = configuration.GetSection(RabbitMqSettings.SectionName);

            opts.Host = GetConfigValue(section, "Host", "RABBITMQ_HOST", "localhost");
            opts.Port = GetConfigValueInt(section, "Port", "RABBITMQ_PORT", 5672);
            opts.Username = GetConfigValue(section, "Username", "RABBITMQ_USER", "guest");
            opts.Password = GetConfigValue(section, "Password", "RABBITMQ_PASSWORD", "guest");
            opts.VirtualHost = GetConfigValue(section, "VirtualHost", "RABBITMQ_VHOST", "/");
            opts.Exchange = GetConfigValue(section, "Exchange", "RABBITMQ_EXCHANGE", "cashflow.events");
            opts.ExchangeType = GetConfigValue(section, "ExchangeType", "RABBITMQ_EXCHANGE_TYPE", "topic");
            opts.AutomaticRecoveryEnabled = GetConfigValueBool(section, "AutomaticRecoveryEnabled", "RABBITMQ_AUTOMATIC_RECOVERY", true);
            opts.NetworkRecoveryInterval = GetConfigValueInt(section, "NetworkRecoveryInterval", "RABBITMQ_NETWORK_RECOVERY_INTERVAL", 10);
        });

        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

        return services;
    }

    /// <summary>
    /// Adiciona os repositórios
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Repositório de Lançamentos
        services.AddScoped<ILancamentoRepository, LancamentoRepository>();

        // Repositório de Saldo Consolidado (com Decorator de Cache)
        services.AddScoped<SaldoConsolidadoRepository>();
        services.AddScoped<ISaldoConsolidadoRepository>(sp =>
        {
            var innerRepository = sp.GetRequiredService<SaldoConsolidadoRepository>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            var logger = sp.GetRequiredService<ILogger<CachedSaldoConsolidadoRepository>>();
            return new CachedSaldoConsolidadoRepository(innerRepository, cacheService, logger);
        });

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
                var factory = CreateRabbitMqConnectionFactory(configuration, rabbitHost);
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

    #region Private Helpers

    private static string GetConnectionString(IConfiguration configuration, string name, string envVar)
    {
        return configuration.GetConnectionString(name)
            ?? Environment.GetEnvironmentVariable(envVar)
            ?? throw new InvalidOperationException($"Connection string '{name}' não encontrada.");
    }

    private static string GetConfigValue(IConfigurationSection section, string key, string envVar, string defaultValue)
    {
        return section[key]
            ?? Environment.GetEnvironmentVariable(envVar)
            ?? defaultValue;
    }

    private static int GetConfigValueInt(IConfigurationSection section, string key, string envVar, int defaultValue)
    {
        var value = section[key] ?? Environment.GetEnvironmentVariable(envVar);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool GetConfigValueBool(IConfigurationSection section, string key, string envVar, bool defaultValue)
    {
        var value = section[key] ?? Environment.GetEnvironmentVariable(envVar);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static ConnectionFactory CreateRabbitMqConnectionFactory(IConfiguration configuration, string host)
    {
        return new ConnectionFactory
        {
            HostName = host,
            Port = GetConfigValueInt(
                configuration.GetSection("RabbitMQ"),
                "Port",
                "RABBITMQ_PORT",
                5672),
            UserName = GetConfigValue(
                configuration.GetSection("RabbitMQ"),
                "Username",
                "RABBITMQ_USER",
                "guest"),
            Password = GetConfigValue(
                configuration.GetSection("RabbitMQ"),
                "Password",
                "RABBITMQ_PASSWORD",
                "guest"),
            VirtualHost = GetConfigValue(
                configuration.GetSection("RabbitMQ"),
                "VirtualHost",
                "RABBITMQ_VHOST",
                "/")
        };
    }

    #endregion
}
