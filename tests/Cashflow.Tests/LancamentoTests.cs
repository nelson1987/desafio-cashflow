using Shouldly;

namespace Cashflow.Tests;

public class LancamentoTests
{
    [Fact]
    public void Deve_Criar_Lancamento_De_Credito_Com_Sucesso()
    {
        // Arrange
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = new DateTime(2024, 1, 15);
        var descricao = "Venda de produto";

        // Act
        var lancamento = new Lancamento(valor, tipo, data, descricao);

        // Assert
        lancamento.Id.ShouldNotBe(Guid.Empty);
        lancamento.Valor.ShouldBe(valor);
        lancamento.Tipo.ShouldBe(tipo);
        lancamento.Data.ShouldBe(data);
        lancamento.Descricao.ShouldBe(descricao);
    }

    [Fact]
    public void Deve_Criar_Lancamento_De_Debito_Com_Sucesso()
    {
        // Arrange
        var valor = 50.00m;
        var tipo = TipoLancamento.Debito;
        var data = new DateTime(2024, 1, 15);
        var descricao = "Pagamento de fornecedor";

        // Act
        var lancamento = new Lancamento(valor, tipo, data, descricao);

        // Assert
        lancamento.Id.ShouldNotBe(Guid.Empty);
        lancamento.Valor.ShouldBe(valor);
        lancamento.Tipo.ShouldBe(tipo);
        lancamento.Descricao.ShouldBe(descricao);
    }

    [Fact]
    public void ValorComSinal_Deve_Ser_Positivo_Para_Credito()
    {
        // Arrange
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, "Venda");

        // Act & Assert
        lancamento.ValorComSinal.ShouldBe(100m);
    }

    [Fact]
    public void ValorComSinal_Deve_Ser_Negativo_Para_Debito()
    {
        // Arrange
        var lancamento = new Lancamento(100m, TipoLancamento.Debito, DateTime.Today, "Compra");

        // Act & Assert
        lancamento.ValorComSinal.ShouldBe(-100m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Deve_Lancar_Excecao_Quando_Valor_For_Invalido(decimal valorInvalido)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(valorInvalido, TipoLancamento.Credito, DateTime.Today, "Teste"))
            .Message.ShouldContain("maior que zero");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deve_Lancar_Excecao_Quando_Descricao_For_Invalida(string? descricaoInvalida)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, descricaoInvalida!))
            .Message.ShouldContain("obrigatória");
    }

    [Fact]
    public void EhDoDia_Deve_Retornar_True_Para_Mesmo_Dia()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15, 10, 30, 0);
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, data, "Venda");

        // Act & Assert
        lancamento.EhDoDia(new DateTime(2024, 1, 15, 23, 59, 59)).ShouldBeTrue();
    }

    [Fact]
    public void EhDoDia_Deve_Retornar_False_Para_Dia_Diferente()
    {
        // Arrange
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, new DateTime(2024, 1, 15), "Venda");

        // Act & Assert
        lancamento.EhDoDia(new DateTime(2024, 1, 16)).ShouldBeFalse();
    }
}