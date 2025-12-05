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

using Consts = Cashflow.Infrastructure.InfrastructureConstants;

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
            var connectionString = GetConnectionString(
                configuration,
                Consts.ConnectionStrings.PostgreSQL,
                Consts.EnvironmentVariables.PostgreSqlConnectionString);

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
        var redisConnection = configuration.GetConnectionString(Consts.ConnectionStrings.Redis)
            ?? Environment.GetEnvironmentVariable(Consts.EnvironmentVariables.RedisConnectionString)
            ?? InfrastructureSettings.Cache.ConnectionStringPadrao;

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

            opts.Host = GetConfigValue(section, Consts.ConfigurationSections.Host, Consts.EnvironmentVariables.RabbitMqHost, Consts.RabbitMqDefaults.Host);
            opts.Port = GetConfigValueInt(section, Consts.ConfigurationSections.Port, Consts.EnvironmentVariables.RabbitMqPort, InfrastructureSettings.RabbitMq.PortaPadrao);
            opts.Username = GetConfigValue(section, Consts.ConfigurationSections.Username, Consts.EnvironmentVariables.RabbitMqUser, Consts.RabbitMqDefaults.Username);
            opts.Password = GetConfigValue(section, Consts.ConfigurationSections.Password, Consts.EnvironmentVariables.RabbitMqPassword, Consts.RabbitMqDefaults.Password);
            opts.VirtualHost = GetConfigValue(section, Consts.ConfigurationSections.VirtualHost, Consts.EnvironmentVariables.RabbitMqVHost, Consts.RabbitMqDefaults.VirtualHost);
            opts.Exchange = GetConfigValue(section, Consts.ConfigurationSections.Exchange, Consts.EnvironmentVariables.RabbitMqExchange, Consts.RabbitMqDefaults.Exchange);
            opts.ExchangeType = GetConfigValue(section, Consts.ConfigurationSections.ExchangeType, Consts.EnvironmentVariables.RabbitMqExchangeType, Consts.RabbitMqDefaults.ExchangeType);
            opts.AutomaticRecoveryEnabled = GetConfigValueBool(section, Consts.ConfigurationSections.AutomaticRecoveryEnabled, Consts.EnvironmentVariables.RabbitMqAutomaticRecovery, true);
            opts.NetworkRecoveryInterval = GetConfigValueInt(section, Consts.ConfigurationSections.NetworkRecoveryInterval, Consts.EnvironmentVariables.RabbitMqNetworkRecoveryInterval, InfrastructureSettings.RabbitMq.NetworkRecoveryIntervalPadrao);
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
        var postgresConnection = configuration.GetConnectionString(Consts.ConnectionStrings.PostgreSQL)
            ?? Environment.GetEnvironmentVariable(Consts.EnvironmentVariables.PostgreSqlConnectionString);

        if (!string.IsNullOrEmpty(postgresConnection))
        {
            healthChecks.AddNpgSql(
                connectionString: postgresConnection,
                name: Consts.HealthChecks.PostgreSqlName,
                tags: Consts.HealthChecks.PostgreSqlTags);
        }

        // Health check do Redis
        var redisConnection = configuration.GetConnectionString(Consts.ConnectionStrings.Redis)
            ?? Environment.GetEnvironmentVariable(Consts.EnvironmentVariables.RedisConnectionString);

        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecks.AddRedis(
                redisConnectionString: redisConnection,
                name: Consts.HealthChecks.RedisName,
                tags: Consts.HealthChecks.RedisTags);
        }

        // Health check do RabbitMQ
        var rabbitHost = configuration.GetSection($"{Consts.ConfigurationSections.RabbitMQ}:{Consts.ConfigurationSections.Host}").Value
            ?? Environment.GetEnvironmentVariable(Consts.EnvironmentVariables.RabbitMqHost);

        if (!string.IsNullOrEmpty(rabbitHost))
        {
            healthChecks.AddRabbitMQ(sp =>
            {
                var factory = CreateRabbitMqConnectionFactory(configuration, rabbitHost);
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            },
            name: Consts.HealthChecks.RabbitMqName,
            tags: Consts.HealthChecks.RabbitMqTags);
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

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChanges"))
        {
            // Se há mudanças pendentes no modelo, usa EnsureCreated como fallback
            await context.Database.EnsureCreatedAsync();
        }
    }

    /// <summary>
    /// Cria o banco de dados sem usar migrations (para testes)
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CashflowDbContext>();
        await context.Database.EnsureCreatedAsync();
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
            ?? throw new InvalidOperationException(string.Format(Consts.ErrorMessages.ConnectionStringNaoEncontrada, name));
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
                configuration.GetSection(Consts.ConfigurationSections.RabbitMQ),
                Consts.ConfigurationSections.Port,
                Consts.EnvironmentVariables.RabbitMqPort,
                InfrastructureSettings.RabbitMq.PortaPadrao),
            UserName = GetConfigValue(
                configuration.GetSection(Consts.ConfigurationSections.RabbitMQ),
                Consts.ConfigurationSections.Username,
                Consts.EnvironmentVariables.RabbitMqUser,
                Consts.RabbitMqDefaults.Username),
            Password = GetConfigValue(
                configuration.GetSection(Consts.ConfigurationSections.RabbitMQ),
                Consts.ConfigurationSections.Password,
                Consts.EnvironmentVariables.RabbitMqPassword,
                Consts.RabbitMqDefaults.Password),
            VirtualHost = GetConfigValue(
                configuration.GetSection(Consts.ConfigurationSections.RabbitMQ),
                Consts.ConfigurationSections.VirtualHost,
                Consts.EnvironmentVariables.RabbitMqVHost,
                Consts.RabbitMqDefaults.VirtualHost)
        };
    }

    #endregion
}