using Shouldly;

namespace Cashflow.Tests;

public class FluxoCaixaTests
{
    [Fact]
    public void Deve_Registrar_Credito_Com_Sucesso()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();

        // Act
        var lancamento = fluxoCaixa.RegistrarCredito(100m, DateTime.Today, "Venda de produto");

        // Assert
        lancamento.Tipo.ShouldBe(TipoLancamento.Credito);
        lancamento.Valor.ShouldBe(100m);
        fluxoCaixa.Lancamentos.Count.ShouldBe(1);
    }

    [Fact]
    public void Deve_Registrar_Debito_Com_Sucesso()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();

        // Act
        var lancamento = fluxoCaixa.RegistrarDebito(50m, DateTime.Today, "Pagamento de conta");

        // Assert
        lancamento.Tipo.ShouldBe(TipoLancamento.Debito);
        lancamento.Valor.ShouldBe(50m);
        fluxoCaixa.Lancamentos.Count.ShouldBe(1);
    }

    [Fact]
    public void Deve_Obter_Saldo_Diario_Consolidado()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();
        var hoje = DateTime.Today;

        fluxoCaixa.RegistrarCredito(500m, hoje, "Venda 1");
        fluxoCaixa.RegistrarCredito(300m, hoje, "Venda 2");
        fluxoCaixa.RegistrarDebito(200m, hoje, "Pagamento fornecedor");

        // Act
        var saldo = fluxoCaixa.ObterSaldoDiario(hoje);

        // Assert
        saldo.TotalCreditos.ShouldBe(800m);
        saldo.TotalDebitos.ShouldBe(200m);
        saldo.Saldo.ShouldBe(600m);
        saldo.QuantidadeLancamentos.ShouldBe(3);
    }

    [Fact]
    public void Deve_Obter_Saldo_Acumulado_Ate_Data()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();
        var dia1 = new DateTime(2024, 1, 15);
        var dia2 = new DateTime(2024, 1, 16);
        var dia3 = new DateTime(2024, 1, 17);

        fluxoCaixa.RegistrarCredito(1000m, dia1, "Venda dia 1");
        fluxoCaixa.RegistrarDebito(300m, dia2, "Compra dia 2");
        fluxoCaixa.RegistrarCredito(500m, dia3, "Venda dia 3");

        // Act
        var saldoDia1 = fluxoCaixa.ObterSaldoAcumulado(dia1);
        var saldoDia2 = fluxoCaixa.ObterSaldoAcumulado(dia2);
        var saldoDia3 = fluxoCaixa.ObterSaldoAcumulado(dia3);

        // Assert
        saldoDia1.ShouldBe(1000m);
        saldoDia2.ShouldBe(700m);   // 1000 - 300
        saldoDia3.ShouldBe(1200m);  // 1000 - 300 + 500
    }

    [Fact]
    public void Deve_Obter_Relatorio_Consolidado_Por_Periodo()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();
        var dataInicio = new DateTime(2024, 1, 15);
        var dataFim = new DateTime(2024, 1, 17);

        fluxoCaixa.RegistrarCredito(1000m, dataInicio, "Venda dia 15");
        fluxoCaixa.RegistrarDebito(200m, dataInicio.AddDays(1), "Compra dia 16");
        fluxoCaixa.RegistrarCredito(500m, dataFim, "Venda dia 17");

        // Act
        var relatorio = fluxoCaixa.ObterRelatorioConsolidado(dataInicio, dataFim).ToList();

        // Assert
        relatorio.Count.ShouldBe(3);

        relatorio[0].Data.ShouldBe(dataInicio);
        relatorio[0].Saldo.ShouldBe(1000m);

        relatorio[1].Data.ShouldBe(dataInicio.AddDays(1));
        relatorio[1].Saldo.ShouldBe(-200m);

        relatorio[2].Data.ShouldBe(dataFim);
        relatorio[2].Saldo.ShouldBe(500m);
    }

    [Fact]
    public void Relatorio_Deve_Incluir_Dias_Sem_Lancamentos()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();
        var dataInicio = new DateTime(2024, 1, 15);
        var dataFim = new DateTime(2024, 1, 17);

        // Apenas lançamento no primeiro dia
        fluxoCaixa.RegistrarCredito(100m, dataInicio, "Venda");

        // Act
        var relatorio = fluxoCaixa.ObterRelatorioConsolidado(dataInicio, dataFim).ToList();

        // Assert
        relatorio.Count.ShouldBe(3);
        relatorio[1].Saldo.ShouldBe(0m); // Dia 16 sem lançamentos
        relatorio[2].Saldo.ShouldBe(0m); // Dia 17 sem lançamentos
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_DataInicio_Maior_Que_DataFim()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();
        var dataInicio = new DateTime(2024, 1, 20);
        var dataFim = new DateTime(2024, 1, 15);

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            fluxoCaixa.ObterRelatorioConsolidado(dataInicio, dataFim));
    }

    [Fact]
    public void Deve_Obter_Lancamentos_Do_Dia()
    {
        // Arrange
        var fluxoCaixa = new FluxoCaixa();
        var hoje = DateTime.Today;
        var ontem = hoje.AddDays(-1);

        fluxoCaixa.RegistrarCredito(100m, hoje, "Venda hoje 1");
        fluxoCaixa.RegistrarCredito(200m, hoje, "Venda hoje 2");
        fluxoCaixa.RegistrarDebito(50m, ontem, "Compra ontem");

        // Act
        var lancamentosHoje = fluxoCaixa.ObterLancamentosDoDia(hoje).ToList();

        // Assert
        lancamentosHoje.Count.ShouldBe(2);
        lancamentosHoje.ShouldAllBe(l => l.EhDoDia(hoje));
    }
}