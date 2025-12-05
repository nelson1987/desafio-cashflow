using Cashflow.Application.DTOs;

namespace Cashflow.WebApi.Extensions;

/// <summary>
/// Extensões para conversão de Result para IResult HTTP
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converte um Result de sucesso para Ok ou erro para BadRequest
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Converte um Result de sucesso para Created ou erro para BadRequest
    /// </summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, string uri)
    {
        return result.IsSuccess
            ? Results.Created(uri, result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Converte um Result para Ok, NotFound ou BadRequest
    /// </summary>
    public static IResult ToHttpResultWithNotFound<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        // Se contém "não encontrado" retorna 404
        var hasNotFound = result.Errors.Any(e =>
            e.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("not found", StringComparison.OrdinalIgnoreCase));

        return hasNotFound
            ? Results.NotFound(new { errors = result.Errors })
            : Results.BadRequest(new { errors = result.Errors });
    }
}