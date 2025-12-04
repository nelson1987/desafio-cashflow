namespace Cashflow.WebApi.Endpoints;

/// <summary>
/// Interface para definição de endpoints da API
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Mapeia o endpoint no roteador da aplicação
    /// </summary>
    static abstract void Map(IEndpointRouteBuilder app);
}

