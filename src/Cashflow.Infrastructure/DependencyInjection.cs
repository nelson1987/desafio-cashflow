using Cashflow.Abstractions;
using Cashflow.Infrastructure.Cache;
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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configura o DbContext com PostgreSQL
        services.AddDbContext<CashflowDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("Connection string 'PostgreSQL' não encontrada.");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                
                npgsqlOptions.CommandTimeout(30);
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
                ?? "localhost:6379";
            options.InstanceName = "Cashflow:";
        });

        // Registra o serviço de cache
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Configura o RabbitMQ
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

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
        var postgresConnection = configuration.GetConnectionString("PostgreSQL");
        if (!string.IsNullOrEmpty(postgresConnection))
        {
            healthChecks.AddNpgSql(
                connectionString: postgresConnection,
                name: "postgresql",
                tags: ["db", "sql", "postgresql"]);
        }

        // Health check do Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecks.AddRedis(
                redisConnectionString: redisConnection,
                name: "redis",
                tags: ["cache", "redis"]);
        }

        // Health check do RabbitMQ
        var rabbitMqSettings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>();
        if (rabbitMqSettings != null)
        {
            healthChecks.AddRabbitMQ(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = rabbitMqSettings.Host,
                    Port = rabbitMqSettings.Port,
                    UserName = rabbitMqSettings.Username,
                    Password = rabbitMqSettings.Password,
                    VirtualHost = rabbitMqSettings.VirtualHost
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

