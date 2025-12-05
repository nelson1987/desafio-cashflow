using Shouldly;
using Xunit;

namespace Cashflow.Tests;

public class SaldoDiarioTests
{
    [Fact]
    public void Criar_ComValoresDiretos_DeveDefinirPropriedadesCorretamente()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var totalCreditos = 1500m;
        var totalDebitos = 500m;
        var quantidade = 10;

        // Act
        var saldo = new SaldoDiario(data, totalCreditos, totalDebitos, quantidade);

        // Assert
        saldo.Data.ShouldBe(data.Date);
        saldo.TotalCreditos.ShouldBe(totalCreditos);
        saldo.TotalDebitos.ShouldBe(totalDebitos);
        saldo.QuantidadeLancamentos.ShouldBe(quantidade);
        saldo.Saldo.ShouldBe(1000m);
    }

    [Fact]
    public void Criar_ComLancamentos_DeveCalcularTotaisCorretamente()
    {
        // Arrange
        var data = DateTime.Today;
        var lancamentos = new List<Lancamento>
        {
            new(100m, TipoLancamento.Credito, data, "Crédito 1"),
            new(200m, TipoLancamento.Credito, data, "Crédito 2"),
            new(50m, TipoLancamento.Debito, data, "Débito 1")
        };

        // Act
        var saldo = new SaldoDiario(data, lancamentos);

        // Assert
        saldo.TotalCreditos.ShouldBe(300m);
        saldo.TotalDebitos.ShouldBe(50m);
        saldo.Saldo.ShouldBe(250m);
        saldo.QuantidadeLancamentos.ShouldBe(3);
    }

    [Fact]
    public void Criar_ComLancamentosDeOutrosDias_DeveFiltrarApenasDoDiaEspecificado()
    {
        // Arrange
        var data = DateTime.Today;
        var lancamentos = new List<Lancamento>
        {
            new(100m, TipoLancamento.Credito, data, "Do dia"),
            new(200m, TipoLancamento.Credito, data.AddDays(-1), "Dia anterior"),
            new(300m, TipoLancamento.Credito, data.AddDays(1), "Dia posterior")
        };

        // Act
        var saldo = new SaldoDiario(data, lancamentos);

        // Assert
        saldo.TotalCreditos.ShouldBe(100m);
        saldo.QuantidadeLancamentos.ShouldBe(1);
    }

    [Fact]
    public void Criar_SemLancamentos_DeveRetornarZeros()
    {
        // Arrange
        var data = DateTime.Today;
        var lancamentos = new List<Lancamento>();

        // Act
        var saldo = new SaldoDiario(data, lancamentos);

        // Assert
        saldo.TotalCreditos.ShouldBe(0m);
        saldo.TotalDebitos.ShouldBe(0m);
        saldo.Saldo.ShouldBe(0m);
        saldo.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public void Criar_ComLancamentosNull_DeveLancarArgumentNullException()
    {
        // Arrange
        var data = DateTime.Today;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SaldoDiario(data, null!));
    }

    [Fact]
    public void Saldo_ComMaisDebitoQueCredito_DeveSerNegativo()
    {
        // Arrange
        var saldo = new SaldoDiario(DateTime.Today, 100m, 500m, 5);

        // Assert
        saldo.Saldo.ShouldBe(-400m);
        saldo.Saldo.ShouldBeLessThan(0);
    }

    [Fact]
    public void Vazio_DeveRetornarSaldoZerado()
    {
        // Arrange
        var data = DateTime.Today;

        // Act
        var saldo = SaldoDiario.Vazio(data);

        // Assert
        saldo.Data.ShouldBe(data.Date);
        saldo.TotalCreditos.ShouldBe(0m);
        saldo.TotalDebitos.ShouldBe(0m);
        saldo.Saldo.ShouldBe(0m);
        saldo.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact]
    public void Criar_ComDataEHora_DeveNormalizarParaData()
    {
        // Arrange
        var dataComHora = new DateTime(2024, 1, 15, 14, 30, 0);

        // Act
        var saldo = new SaldoDiario(dataComHora, 100m, 50m, 1);

        // Assert
        saldo.Data.ShouldBe(new DateTime(2024, 1, 15));
        saldo.Data.TimeOfDay.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void JsonConstructor_DeveCriarInstanciaVazia()
    {
        // Act
        var saldo = new SaldoDiario();

        // Assert
        saldo.Data.ShouldBe(default);
        saldo.TotalCreditos.ShouldBe(0m);
        saldo.TotalDebitos.ShouldBe(0m);
        saldo.QuantidadeLancamentos.ShouldBe(0);
    }
}
