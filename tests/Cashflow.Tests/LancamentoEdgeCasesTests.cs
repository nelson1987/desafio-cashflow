using Shouldly;

using Xunit;

namespace Cashflow.Tests;

public class LancamentoEdgeCasesTests
{
    #region Validação de Valor

    [Fact]
    public void Criar_ComValorZero_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(0m, TipoLancamento.Credito, DateTime.Today, "Teste"));
    }

    [Fact]
    public void Criar_ComValorNegativo_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(-100m, TipoLancamento.Credito, DateTime.Today, "Teste"));
    }

    [Fact]
    public void Criar_ComValorMuitoPequeno_DeveSerValido()
    {
        // Arrange & Act
        var lancamento = new Lancamento(0.01m, TipoLancamento.Credito, DateTime.Today, "Centavo");

        // Assert
        lancamento.Valor.ShouldBe(0.01m);
    }

    [Fact]
    public void Criar_ComValorMuitoGrande_DeveSerValido()
    {
        // Arrange & Act
        var lancamento = new Lancamento(decimal.MaxValue / 2, TipoLancamento.Credito, DateTime.Today, "Grande");

        // Assert
        lancamento.Valor.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Validação de Descrição

    [Fact]
    public void Criar_ComDescricaoVazia_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, ""));
    }

    [Fact]
    public void Criar_ComDescricaoApenasEspacos_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, "   "));
    }

    [Fact]
    public void Criar_ComDescricaoNull_DeveLancarArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, null!));
    }

    [Fact]
    public void Criar_ComDescricaoMuitoLonga_DeveSerValido()
    {
        // Arrange
        var descricaoLonga = new string('a', 1000);

        // Act
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, descricaoLonga);

        // Assert
        lancamento.Descricao.Length.ShouldBe(1000);
    }

    [Fact]
    public void Criar_ComDescricaoComCaracteresEspeciais_DeveSerValido()
    {
        // Arrange
        var descricao = "Pagamento R$ 100,00 - NF #12345 @empresa 'teste' \"aspas\"";

        // Act
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, descricao);

        // Assert
        lancamento.Descricao.ShouldBe(descricao);
    }

    #endregion

    #region Normalização de Data

    [Fact]
    public void Criar_ComDataEHora_DeveNormalizarParaData()
    {
        // Arrange
        var dataComHora = new DateTime(2024, 1, 15, 14, 30, 45);

        // Act
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, dataComHora, "Teste");

        // Assert
        lancamento.Data.Hour.ShouldBe(0);
        lancamento.Data.Minute.ShouldBe(0);
        lancamento.Data.Second.ShouldBe(0);
    }

    [Fact]
    public void Criar_ComDataMinima_DeveSerValido()
    {
        // Arrange
        var dataMinima = new DateTime(1900, 1, 1);

        // Act
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, dataMinima, "Antigo");

        // Assert
        lancamento.Data.ShouldBe(dataMinima);
    }

    [Fact]
    public void Criar_ComDataFutura_DeveSerValido()
    {
        // Arrange
        var dataFutura = DateTime.Today.AddYears(10);

        // Act
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, dataFutura, "Futuro");

        // Assert
        lancamento.Data.ShouldBe(dataFutura);
    }

    #endregion

    #region ValorComSinal

    [Theory]
    [InlineData(100, TipoLancamento.Credito, 100)]
    [InlineData(100, TipoLancamento.Debito, -100)]
    [InlineData(0.01, TipoLancamento.Credito, 0.01)]
    [InlineData(0.01, TipoLancamento.Debito, -0.01)]
    public void ValorComSinal_DeveCalcularCorretamente(decimal valor, TipoLancamento tipo, decimal esperado)
    {
        // Arrange
        var lancamento = new Lancamento(valor, tipo, DateTime.Today, "Teste");

        // Assert
        lancamento.ValorComSinal.ShouldBe(esperado);
    }

    #endregion

    #region EhDoDia

    [Fact]
    public void EhDoDia_ComMesmaData_DeveRetornarTrue()
    {
        // Arrange
        var data = DateTime.Today;
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, data, "Teste");

        // Act & Assert
        lancamento.EhDoDia(data).ShouldBeTrue();
    }

    [Fact]
    public void EhDoDia_ComDataDiferente_DeveRetornarFalse()
    {
        // Arrange
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, "Teste");

        // Act & Assert
        lancamento.EhDoDia(DateTime.Today.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void EhDoDia_ComMesmaDataMasHoraDiferente_DeveRetornarTrue()
    {
        // Arrange
        var data = DateTime.Today;
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, data, "Teste");
        var dataComHora = data.AddHours(15);

        // Act & Assert
        lancamento.EhDoDia(dataComHora).ShouldBeTrue();
    }

    #endregion

    #region ID Único

    [Fact]
    public void Criar_MultiplosLancamentos_DevemTerIDsUnicos()
    {
        // Arrange & Act
        var lancamentos = Enumerable.Range(1, 100)
            .Select(i => new Lancamento(i * 10m, TipoLancamento.Credito, DateTime.Today, $"Lancamento {i}"))
            .ToList();

        // Assert
        var idsUnicos = lancamentos.Select(l => l.Id).Distinct().Count();
        idsUnicos.ShouldBe(100);
    }

    #endregion
}