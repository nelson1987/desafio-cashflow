using Cashflow.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.Application.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using static Cashflow.DomainConstants;

namespace Cashflow.Application.Tests.Services;

public class ConsolidadoServiceTests
{
    private readonly Mock<ISaldoConsolidadoRepository> _repositoryMock;
    private readonly Mock<ILogger<ConsolidadoService>> _loggerMock;
    private readonly ConsolidadoService _sut;

    public ConsolidadoServiceTests()
    {
        _repositoryMock = new Mock<ISaldoConsolidadoRepository>();
        _loggerMock = new Mock<ILogger<ConsolidadoService>>();

        _sut = new ConsolidadoService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region ObterPorDataAsync

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarSaldo_QuandoExiste()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var saldo = new SaldoDiario(data, 1000m, 300m, 5);

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldo);

        // Act
        var result = await _sut.ObterPorDataAsync(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Data.ShouldBe(data);
        result.Value.TotalCreditos.ShouldBe(1000m);
        result.Value.TotalDebitos.ShouldBe(300m);
        result.Value.Saldo.ShouldBe(700m);
        result.Value.QuantidadeLancamentos.ShouldBe(5);
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarSaldoVazio_QuandoNaoExiste()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaldoDiario?)null);

        // Act
        var result = await _sut.ObterPorDataAsync(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Data.ShouldBe(data);
        result.Value.TotalCreditos.ShouldBe(0m);
        result.Value.TotalDebitos.ShouldBe(0m);
        result.Value.Saldo.ShouldBe(0m);
        result.Value.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        var data = DateTime.Today;

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // Act
        var result = await _sut.ObterPorDataAsync(data);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosConsolidado.ErroAoBuscar);
    }

    #endregion

    #region ObterPorPeriodoAsync

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarRelatorio_QuandoPeriodoValido()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 1);
        var dataFim = new DateTime(2024, 1, 3);
        var saldos = new List<SaldoDiario>
        {
            new(new DateTime(2024, 1, 1), 500m, 100m, 3),
            new(new DateTime(2024, 1, 3), 300m, 50m, 2)
        };

        _repositoryMock
            .Setup(r => r.ObterPorPeriodoAsync(dataInicio, dataFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldos);

        // Act
        var result = await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.DataInicio.ShouldBe(dataInicio.Date);
        result.Value.DataFim.ShouldBe(dataFim.Date);
        result.Value.Saldos.Count().ShouldBe(3); // 3 dias (incluindo dia sem movimentação)
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DevePreencherDiasSemMovimentacao()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 1);
        var dataFim = new DateTime(2024, 1, 5);
        var saldos = new List<SaldoDiario>
        {
            new(new DateTime(2024, 1, 1), 500m, 100m, 3),
            new(new DateTime(2024, 1, 5), 300m, 50m, 2)
        };

        _repositoryMock
            .Setup(r => r.ObterPorPeriodoAsync(dataInicio, dataFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldos);

        // Act
        var result = await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var saldosList = result.Value.Saldos.ToList();
        saldosList.Count.ShouldBe(5); // 5 dias no período

        // Dias com movimentação
        saldosList[0].QuantidadeLancamentos.ShouldBe(3);
        saldosList[4].QuantidadeLancamentos.ShouldBe(2);

        // Dias sem movimentação (02, 03 e 04)
        saldosList[1].QuantidadeLancamentos.ShouldBe(0);
        saldosList[2].QuantidadeLancamentos.ShouldBe(0);
        saldosList[3].QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveCalcularResumoCorreto()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 1);
        var dataFim = new DateTime(2024, 1, 2);
        var saldos = new List<SaldoDiario>
        {
            new(new DateTime(2024, 1, 1), 500m, 100m, 3),
            new(new DateTime(2024, 1, 2), 300m, 50m, 2)
        };

        _repositoryMock
            .Setup(r => r.ObterPorPeriodoAsync(dataInicio, dataFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldos);

        // Act
        var result = await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var resumo = result.Value.Resumo;
        resumo.TotalCreditos.ShouldBe(800m);  // 500 + 300
        resumo.TotalDebitos.ShouldBe(150m);   // 100 + 50
        resumo.SaldoFinal.ShouldBe(650m);     // (500-100) + (300-50)
        resumo.TotalLancamentos.ShouldBe(5); // 3 + 2
        resumo.DiasComMovimentacao.ShouldBe(2);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarFalha_QuandoDataInicialMaiorQueFinal()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 15);
        var dataFim = new DateTime(2024, 1, 1);

        // Act
        var result = await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosConsolidado.DataInicialMaiorQueFinal);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarFalha_QuandoPeriodoExcede90Dias()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 1);
        var dataFim = dataInicio.AddDays(Consolidacao.PeriodoMaximoDias + 1);

        // Act
        var result = await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(Consolidacao.PeriodoMaximoDias.ToString());
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 1);
        var dataFim = new DateTime(2024, 1, 10);

        _repositoryMock
            .Setup(r => r.ObterPorPeriodoAsync(dataInicio, dataFim, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro"));

        // Act
        var result = await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosConsolidado.ErroAoGerarRelatorio);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_NaoDeveChamarRepositorio_QuandoDataInicialMaiorQueFinal()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 15);
        var dataFim = new DateTime(2024, 1, 1);

        // Act
        await _sut.ObterPorPeriodoAsync(dataInicio, dataFim);

        // Assert
        _repositoryMock.Verify(
            r => r.ObterPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region RecalcularAsync

    [Fact]
    public async Task RecalcularAsync_DeveRetornarSaldoRecalculado()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var saldoRecalculado = new SaldoDiario(data, 1500m, 500m, 8);

        _repositoryMock
            .Setup(r => r.RecalcularAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saldoRecalculado);

        // Act
        var result = await _sut.RecalcularAsync(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Data.ShouldBe(data);
        result.Value.TotalCreditos.ShouldBe(1500m);
        result.Value.TotalDebitos.ShouldBe(500m);
        result.Value.Saldo.ShouldBe(1000m);
        result.Value.QuantidadeLancamentos.ShouldBe(8);
    }

    [Fact]
    public async Task RecalcularAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        var data = DateTime.Today;

        _repositoryMock
            .Setup(r => r.RecalcularAsync(data, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro de recálculo"));

        // Act
        var result = await _sut.RecalcularAsync(data);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosConsolidado.ErroAoRecalcular);
    }

    #endregion
}

