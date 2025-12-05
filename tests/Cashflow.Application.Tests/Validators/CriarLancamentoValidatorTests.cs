using Cashflow.Application.DTOs;
using Cashflow.Application.Validators;

using Shouldly;

using static Cashflow.Application.ApplicationConstants;
using static Cashflow.DomainConstants;

namespace Cashflow.Application.Tests.Validators;

public class CriarLancamentoValidatorTests
{
    private readonly CriarLancamentoValidator _sut;

    public CriarLancamentoValidatorTests()
    {
        _sut = new CriarLancamentoValidator();
    }

    #region Valor

    [Fact]
    public async Task Deve_Passar_Quando_ValorMaiorQueZero()
    {
        // Arrange
        var request = CriarRequestValido() with { Valor = 100.50m };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public async Task Deve_Falhar_Quando_ValorMenorOuIgualZero(decimal valor)
    {
        // Arrange
        var request = CriarRequestValido() with { Valor = valor };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ValidacaoLancamento.ValorDeveSerMaiorQueZero);
    }

    [Fact]
    public async Task Deve_Passar_Quando_ValorMinimoPositivo()
    {
        // Arrange
        var request = CriarRequestValido() with { Valor = 0.01m };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion

    #region Tipo

    [Theory]
    [InlineData(TipoLancamento.Credito)]
    [InlineData(TipoLancamento.Debito)]
    public async Task Deve_Passar_Quando_TipoValido(TipoLancamento tipo)
    {
        // Arrange
        var request = CriarRequestValido() with { Tipo = tipo };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task Deve_Falhar_Quando_TipoInvalido(int tipoInvalido)
    {
        // Arrange
        var request = CriarRequestValido() with { Tipo = (TipoLancamento)tipoInvalido };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Tipo");
    }

    #endregion

    #region Data

    [Fact]
    public async Task Deve_Passar_Quando_DataHoje()
    {
        // Arrange
        var request = CriarRequestValido() with { Data = DateTime.Today };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Deve_Passar_Quando_DataOntem()
    {
        // Arrange
        var request = CriarRequestValido() with { Data = DateTime.Today.AddDays(-1) };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Deve_Passar_Quando_DataAmanha()
    {
        // Arrange - Permite até 1 dia no futuro conforme LancamentoLimites.DiasPermitidosFuturos
        var request = CriarRequestValido() with { Data = DateTime.Today.AddDays(LancamentoLimites.DiasPermitidosFuturos) };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Deve_Falhar_Quando_DataMuitoFutura()
    {
        // Arrange - Mais de 1 dia no futuro
        var request = CriarRequestValido() with { Data = DateTime.Today.AddDays(LancamentoLimites.DiasPermitidosFuturos + 1) };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ValidacaoLancamento.DataNaoPodeFutura);
    }

    [Fact]
    public async Task Deve_Falhar_Quando_DataVazia()
    {
        // Arrange
        var request = CriarRequestValido() with { Data = default };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ValidacaoLancamento.DataObrigatoria);
    }

    #endregion

    #region Descricao

    [Fact]
    public async Task Deve_Passar_Quando_DescricaoValida()
    {
        // Arrange
        var request = CriarRequestValido() with { Descricao = "Venda de produto" };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Deve_Falhar_Quando_DescricaoVaziaOuNula(string? descricao)
    {
        // Arrange
        var request = CriarRequestValido() with { Descricao = descricao! };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ValidacaoLancamento.DescricaoObrigatoria);
    }

    [Fact]
    public async Task Deve_Passar_Quando_DescricaoNoLimiteMaximo()
    {
        // Arrange
        var descricao = new string('a', LancamentoLimites.DescricaoMaxLength);
        var request = CriarRequestValido() with { Descricao = descricao };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Deve_Falhar_Quando_DescricaoExcedeLimiteMaximo()
    {
        // Arrange
        var descricao = new string('a', LancamentoLimites.DescricaoMaxLength + 1);
        var request = CriarRequestValido() with { Descricao = descricao };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Descricao");
    }

    #endregion

    #region Múltiplas Validações

    [Fact]
    public async Task Deve_RetornarMultiplosErros_QuandoMultiplosCamposInvalidos()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 0,
            Tipo = (TipoLancamento)99,
            Data = default,
            Descricao = ""
        };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task Deve_Passar_Quando_TodosCamposValidos()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 1500.75m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Venda de serviços de consultoria"
        };

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Helpers

    private static CriarLancamentoRequest CriarRequestValido() => new()
    {
        Valor = 100m,
        Tipo = TipoLancamento.Credito,
        Data = DateTime.Today,
        Descricao = "Lançamento de teste"
    };

    #endregion
}

