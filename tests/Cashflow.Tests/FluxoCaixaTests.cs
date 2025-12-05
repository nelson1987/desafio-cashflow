using Shouldly;

using Xunit;

namespace Cashflow.Tests;

public class FluxoCaixaTests
{
    private readonly FluxoCaixa _fluxoCaixa;

    public FluxoCaixaTests()
    {
        _fluxoCaixa = new FluxoCaixa();
    }

    #region RegistrarCredito

    [Fact]
    public void RegistrarCredito_DeveAdicionarLancamentoALista()
    {
        // Act
        var lancamento = _fluxoCaixa.RegistrarCredito(100m, DateTime.Today, "Venda");

        // Assert
        _fluxoCaixa.Lancamentos.ShouldContain(lancamento);
        _fluxoCaixa.Lancamentos.Count.ShouldBe(1);
    }

    [Fact]
    public void RegistrarCredito_DeveRetornarLancamentoComTipoCredito()
    {
        // Act
        var lancamento = _fluxoCaixa.RegistrarCredito(100m, DateTime.Today, "Venda");

        // Assert
        lancamento.Tipo.ShouldBe(TipoLancamento.Credito);
    }

    #endregion

    #region RegistrarDebito

    [Fact]
    public void RegistrarDebito_DeveAdicionarLancamentoALista()
    {
        // Act
        var lancamento = _fluxoCaixa.RegistrarDebito(50m, DateTime.Today, "Compra");

        // Assert
        _fluxoCaixa.Lancamentos.ShouldContain(lancamento);
        _fluxoCaixa.Lancamentos.Count.ShouldBe(1);
    }

    [Fact]
    public void RegistrarDebito_DeveRetornarLancamentoComTipoDebito()
    {
        // Act
        var lancamento = _fluxoCaixa.RegistrarDebito(50m, DateTime.Today, "Compra");

        // Assert
        lancamento.Tipo.ShouldBe(TipoLancamento.Debito);
    }

    #endregion

    #region ObterSaldoDiario

    [Fact]
    public void ObterSaldoDiario_SemLancamentos_DeveRetornarSaldoZerado()
    {
        // Act
        var saldo = _fluxoCaixa.ObterSaldoDiario(DateTime.Today);

        // Assert
        saldo.Saldo.ShouldBe(0m);
        saldo.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public void ObterSaldoDiario_ComLancamentos_DeveCalcularCorretamente()
    {
        // Arrange
        _fluxoCaixa.RegistrarCredito(500m, DateTime.Today, "Venda 1");
        _fluxoCaixa.RegistrarCredito(300m, DateTime.Today, "Venda 2");
        _fluxoCaixa.RegistrarDebito(200m, DateTime.Today, "Compra 1");

        // Act
        var saldo = _fluxoCaixa.ObterSaldoDiario(DateTime.Today);

        // Assert
        saldo.TotalCreditos.ShouldBe(800m);
        saldo.TotalDebitos.ShouldBe(200m);
        saldo.Saldo.ShouldBe(600m);
        saldo.QuantidadeLancamentos.ShouldBe(3);
    }

    #endregion

    #region ObterRelatorioConsolidado

    [Fact]
    public void ObterRelatorioConsolidado_DeveRetornarSaldoParaCadaDia()
    {
        // Arrange
        var dataInicio = DateTime.Today;
        var dataFim = DateTime.Today.AddDays(2);
        _fluxoCaixa.RegistrarCredito(100m, dataInicio, "Venda dia 1");
        _fluxoCaixa.RegistrarCredito(200m, dataInicio.AddDays(1), "Venda dia 2");

        // Act
        var relatorio = _fluxoCaixa.ObterRelatorioConsolidado(dataInicio, dataFim).ToList();

        // Assert
        relatorio.Count.ShouldBe(3);
        relatorio[0].TotalCreditos.ShouldBe(100m);
        relatorio[1].TotalCreditos.ShouldBe(200m);
        relatorio[2].TotalCreditos.ShouldBe(0m);
    }

    [Fact]
    public void ObterRelatorioConsolidado_ComDataInicioMaiorQueFim_DeveLancarExcecao()
    {
        // Arrange
        var dataInicio = DateTime.Today.AddDays(5);
        var dataFim = DateTime.Today;

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _fluxoCaixa.ObterRelatorioConsolidado(dataInicio, dataFim));
    }

    [Fact]
    public void ObterRelatorioConsolidado_ComMesmaData_DeveRetornarUmDia()
    {
        // Arrange
        var data = DateTime.Today;

        // Act
        var relatorio = _fluxoCaixa.ObterRelatorioConsolidado(data, data).ToList();

        // Assert
        relatorio.Count.ShouldBe(1);
    }

    #endregion

    #region ObterSaldoAcumulado

    [Fact]
    public void ObterSaldoAcumulado_SemLancamentos_DeveRetornarZero()
    {
        // Act
        var saldo = _fluxoCaixa.ObterSaldoAcumulado(DateTime.Today);

        // Assert
        saldo.ShouldBe(0m);
    }

    [Fact]
    public void ObterSaldoAcumulado_DeveConsiderarTodosLancamentosAteData()
    {
        // Arrange
        _fluxoCaixa.RegistrarCredito(100m, DateTime.Today.AddDays(-2), "Anterior");
        _fluxoCaixa.RegistrarCredito(200m, DateTime.Today.AddDays(-1), "Ontem");
        _fluxoCaixa.RegistrarDebito(50m, DateTime.Today, "Hoje");
        _fluxoCaixa.RegistrarCredito(1000m, DateTime.Today.AddDays(1), "Amanhã");

        // Act
        var saldo = _fluxoCaixa.ObterSaldoAcumulado(DateTime.Today);

        // Assert
        saldo.ShouldBe(250m); // 100 + 200 - 50 = 250 (exclui amanhã)
    }

    #endregion

    #region ObterLancamentosDoDia

    [Fact]
    public void ObterLancamentosDoDia_SemLancamentos_DeveRetornarVazio()
    {
        // Act
        var lancamentos = _fluxoCaixa.ObterLancamentosDoDia(DateTime.Today);

        // Assert
        lancamentos.ShouldBeEmpty();
    }

    [Fact]
    public void ObterLancamentosDoDia_DeveRetornarApenasLancamentosDoDia()
    {
        // Arrange
        _fluxoCaixa.RegistrarCredito(100m, DateTime.Today, "Hoje");
        _fluxoCaixa.RegistrarCredito(200m, DateTime.Today.AddDays(-1), "Ontem");
        _fluxoCaixa.RegistrarDebito(50m, DateTime.Today, "Hoje também");

        // Act
        var lancamentos = _fluxoCaixa.ObterLancamentosDoDia(DateTime.Today).ToList();

        // Assert
        lancamentos.Count.ShouldBe(2);
        lancamentos.All(l => l.Data == DateTime.Today.Date).ShouldBeTrue();
    }

    #endregion

    #region Cenários Integrados

    [Fact]
    public void FluxoCaixa_ComMultiplosLancamentos_DeveManterConsistencia()
    {
        // Arrange - Simula uma semana de operações
        var inicio = DateTime.Today.AddDays(-6);

        for (int i = 0; i < 7; i++)
        {
            var data = inicio.AddDays(i);
            _fluxoCaixa.RegistrarCredito(100m * (i + 1), data, $"Venda dia {i + 1}");
            _fluxoCaixa.RegistrarDebito(50m, data, $"Despesa dia {i + 1}");
        }

        // Act
        var relatorio = _fluxoCaixa.ObterRelatorioConsolidado(inicio, DateTime.Today).ToList();
        var saldoAcumulado = _fluxoCaixa.ObterSaldoAcumulado(DateTime.Today);

        // Assert
        relatorio.Count.ShouldBe(7);
        _fluxoCaixa.Lancamentos.Count.ShouldBe(14);

        // Créditos: 100+200+300+400+500+600+700 = 2800
        // Débitos: 50*7 = 350
        // Saldo: 2800 - 350 = 2450
        saldoAcumulado.ShouldBe(2450m);
    }

    #endregion
}