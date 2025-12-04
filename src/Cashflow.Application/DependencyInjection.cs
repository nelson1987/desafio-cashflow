using Cashflow.Application.Abstractions;
using Cashflow.Application.Services;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Cashflow.Application;

/// <summary>
/// Extensões para configuração da camada de aplicação
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona todos os serviços da camada de aplicação
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddApplicationServices()
            .AddValidators();

        return services;
    }

    /// <summary>
    /// Adiciona os serviços de aplicação (Use Cases)
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ILancamentoService, LancamentoService>();
        services.AddScoped<IConsolidadoService, ConsolidadoService>();

        return services;
    }

    /// <summary>
    /// Adiciona os validadores FluentValidation
    /// </summary>
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        // Registra todos os validadores do assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}

