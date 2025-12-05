using Cashflow.Application.DTOs;
using Shouldly;
using Xunit;

namespace Cashflow.Application.Tests.DTOs;

public class SaldoConsolidadoDtoTests
{
    [Fact]
    public void SaldoConsolidadoResponse_DeveSerCriadoComValoresCorretos()
    {
        // Arrange & Act
        var response = new SaldoConsolidadoResponse
        {
            Data = DateTime.UtcNow.Date,
            TotalCreditos = 1500m,
            TotalDebitos = 500m,
            Saldo = 1000m,
            QuantidadeLancamentos = 10
        };

        // Assert
        response.TotalCreditos.ShouldBe(1500m);
        response.TotalDebitos.ShouldBe(500m);
        response.Saldo.ShouldBe(1000m);
        response.QuantidadeLancamentos.ShouldBe(10);
    }

    [Fact]
    public void SaldoConsolidadoResponse_FromDomain_DeveConverterCorretamente()
    {
        // Arrange
        var saldo = new SaldoDiario(DateTime.UtcNow.Date, 2000m, 800m, 5);

        // Act
        var response = SaldoConsolidadoResponse.FromDomain(saldo);

        // Assert
        response.Data.ShouldBe(saldo.Data);
        response.TotalCreditos.ShouldBe(2000m);
        response.TotalDebitos.ShouldBe(800m);
        response.Saldo.ShouldBe(1200m);
        response.QuantidadeLancamentos.ShouldBe(5);
    }

    [Fact]
    public void SaldoConsolidadoResponse_Vazio_DeveRetornarSaldoZerado()
    {
        // Arrange
        var data = DateTime.UtcNow.Date;

        // Act
        var response = SaldoConsolidadoResponse.Vazio(data);

        // Assert
        response.Data.ShouldBe(data);
        response.TotalCreditos.ShouldBe(0m);
        response.TotalDebitos.ShouldBe(0m);
        response.Saldo.ShouldBe(0m);
        response.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public void SaldoConsolidadoResponse_ComSaldoNegativo_DeveSerValido()
    {
        // Arrange & Act
        var response = new SaldoConsolidadoResponse
        {
            Data = DateTime.UtcNow.Date,
            TotalCreditos = 500m,
            TotalDebitos = 1500m,
            Saldo = -1000m,
            QuantidadeLancamentos = 5
        };

        // Assert
        response.Saldo.ShouldBe(-1000m);
        response.Saldo.ShouldBeLessThan(0);
    }

    [Fact]
    public void RelatorioConsolidadoResponse_DeveConterListaDeSaldos()
    {
        // Arrange
        var saldos = new List<SaldoConsolidadoResponse>
        {
            new() { Data = DateTime.UtcNow.Date.AddDays(-2), TotalCreditos = 100m, TotalDebitos = 50m, Saldo = 50m, QuantidadeLancamentos = 2 },
            new() { Data = DateTime.UtcNow.Date.AddDays(-1), TotalCreditos = 200m, TotalDebitos = 75m, Saldo = 125m, QuantidadeLancamentos = 3 },
            new() { Data = DateTime.UtcNow.Date, TotalCreditos = 150m, TotalDebitos = 25m, Saldo = 125m, QuantidadeLancamentos = 2 }
        };

        // Act
        var response = new RelatorioConsolidadoResponse
        {
            Saldos = saldos,
            DataInicio = DateTime.UtcNow.Date.AddDays(-2),
            DataFim = DateTime.UtcNow.Date
        };

        // Assert
        response.Saldos.Count().ShouldBe(3);
        response.DataInicio.ShouldBe(DateTime.UtcNow.Date.AddDays(-2));
        response.DataFim.ShouldBe(DateTime.UtcNow.Date);
    }

    [Fact]
    public void ResumoConsolidadoResponse_DeveConterTotais()
    {
        // Act
        var resumo = new ResumoConsolidadoResponse
        {
            TotalCreditos = 450m,
            TotalDebitos = 150m,
            SaldoFinal = 300m,
            TotalLancamentos = 7,
            DiasComMovimentacao = 3
        };

        // Assert
        resumo.TotalCreditos.ShouldBe(450m);
        resumo.TotalDebitos.ShouldBe(150m);
        resumo.SaldoFinal.ShouldBe(300m);
        resumo.TotalLancamentos.ShouldBe(7);
        resumo.DiasComMovimentacao.ShouldBe(3);
    }

    [Fact]
    public void RelatorioConsolidadoResponse_ComResumo_DeveConterDadosCompletos()
    {
        // Arrange
        var saldos = new List<SaldoConsolidadoResponse>
        {
            new() { Data = DateTime.UtcNow.Date.AddDays(-1), TotalCreditos = 100m, TotalDebitos = 50m, Saldo = 50m, QuantidadeLancamentos = 2 },
            new() { Data = DateTime.UtcNow.Date, TotalCreditos = 200m, TotalDebitos = 75m, Saldo = 125m, QuantidadeLancamentos = 3 }
        };

        var resumo = new ResumoConsolidadoResponse
        {
            TotalCreditos = 300m,
            TotalDebitos = 125m,
            SaldoFinal = 175m,
            TotalLancamentos = 5,
            DiasComMovimentacao = 2
        };

        // Act
        var response = new RelatorioConsolidadoResponse
        {
            Saldos = saldos,
            DataInicio = DateTime.UtcNow.Date.AddDays(-1),
            DataFim = DateTime.UtcNow.Date,
            Resumo = resumo
        };

        // Assert
        response.Saldos.Count().ShouldBe(2);
        response.Resumo.TotalCreditos.ShouldBe(300m);
        response.Resumo.SaldoFinal.ShouldBe(175m);
    }
}
