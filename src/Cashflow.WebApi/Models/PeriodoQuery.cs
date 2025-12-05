using Microsoft.AspNetCore.Mvc;

namespace Cashflow.WebApi.Models;

/// <summary>
/// Parâmetros para consulta por período
/// </summary>
public record PeriodoQuery
{
    /// <summary>
    /// Data de início do período
    /// </summary>
    [FromQuery(Name = "dataInicio")]
    public DateTime DataInicio { get; init; }

    /// <summary>
    /// Data de fim do período
    /// </summary>
    [FromQuery(Name = "dataFim")]
    public DateTime DataFim { get; init; }
}