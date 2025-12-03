using Shouldly;

namespace Cashflow.Tests;

public class SaldoDiarioTests
{
    [Fact]
    public void Deve_Calcular_Saldo_Corretamente()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var lancamentos = new List<Lancamento>
        {
            new(200m, TipoLancamento.Credito, data, "Venda 1"),
            new(150m, TipoLancamento.Credito, data, "Venda 2"),
            new(100m, TipoLancamento.Debito, data, "Compra 1")
        };

        // Act
        var saldoDiario = new SaldoDiario(data, lancamentos);

        // Assert
        saldoDiario.TotalCreditos.ShouldBe(350m);
        saldoDiario.TotalDebitos.ShouldBe(100m);
        saldoDiario.Saldo.ShouldBe(250m);
        saldoDiario.QuantidadeLancamentos.ShouldBe(3);
    }

    [Fact]
    public void Deve_Ignorar_Lancamentos_De_Outros_Dias()
    {
        // Arrange
        var dataAlvo = new DateTime(2024, 1, 15);
        var lancamentos = new List<Lancamento>
        {
            new(200m, TipoLancamento.Credito, dataAlvo, "Venda do dia"),
            new(500m, TipoLancamento.Credito, dataAlvo.AddDays(-1), "Venda dia anterior"),
            new(300m, TipoLancamento.Debito, dataAlvo.AddDays(1), "Compra dia seguinte")
        };

        // Act
        var saldoDiario = new SaldoDiario(dataAlvo, lancamentos);

        // Assert
        saldoDiario.TotalCreditos.ShouldBe(200m);
        saldoDiario.TotalDebitos.ShouldBe(0m);
        saldoDiario.Saldo.ShouldBe(200m);
        saldoDiario.QuantidadeLancamentos.ShouldBe(1);
    }

    [Fact]
    public void Deve_Retornar_Saldo_Zero_Quando_Nao_Houver_Lancamentos()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var lancamentos = new List<Lancamento>();

        // Act
        var saldoDiario = new SaldoDiario(data, lancamentos);

        // Assert
        saldoDiario.Saldo.ShouldBe(0m);
        saldoDiario.TotalCreditos.ShouldBe(0m);
        saldoDiario.TotalDebitos.ShouldBe(0m);
        saldoDiario.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public void Deve_Retornar_Saldo_Negativo_Quando_Debitos_Maiores()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var lancamentos = new List<Lancamento>
        {
            new(100m, TipoLancamento.Credito, data, "Venda"),
            new(300m, TipoLancamento.Debito, data, "Pagamento fornecedor")
        };

        // Act
        var saldoDiario = new SaldoDiario(data, lancamentos);

        // Assert
        saldoDiario.Saldo.ShouldBe(-200m);
    }

    [Fact]
    public void Vazio_Deve_Criar_Saldo_Zerado()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);

        // Act
        var saldoDiario = SaldoDiario.Vazio(data);

        // Assert
        saldoDiario.Data.ShouldBe(data);
        saldoDiario.Saldo.ShouldBe(0m);
        saldoDiario.TotalCreditos.ShouldBe(0m);
        saldoDiario.TotalDebitos.ShouldBe(0m);
        saldoDiario.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Lancamentos_For_Nulo()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SaldoDiario(DateTime.Today, null!));
    }
}

