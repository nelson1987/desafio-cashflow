using Cashflow.Application.DTOs;
using Cashflow.Application.Validators;
using FluentValidation.TestHelper;
using Shouldly;
using Xunit;

namespace Cashflow.Application.Tests.Validators;

public class ValidatorEdgeCasesTests
{
    private readonly CriarLancamentoValidator _validator;

    public ValidatorEdgeCasesTests()
    {
        _validator = new CriarLancamentoValidator();
    }

    #region Valor Edge Cases

    [Fact]
    public void Valor_ComMuitasCasasDecimais_DeveSerValido()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100.99m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Valor_ComValorMuitoGrande_DeveSerValido()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 999999999.99m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.001)]
    [InlineData(0.10)]
    public void Valor_ComValorMuitoPequeno_DeveSerValido(decimal valor)
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = valor,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void Valor_ComZero_DeveSerInvalido()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 0m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Valor);
    }

    #endregion

    #region Descricao Edge Cases

    [Fact]
    public void Descricao_ComCaracteresEspeciais_DeveSerValida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Pagamento R$ 100,00 - NF #12345 @empresa"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Descricao);
    }

    [Fact]
    public void Descricao_ComEmoji_DeveSerValida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Venda 🎉"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Descricao);
    }

    [Fact]
    public void Descricao_ComQuebraLinha_DeveSerValida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Linha 1\nLinha 2"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Descricao);
    }

    [Fact]
    public void Descricao_ComApenasEspacos_DeveSerInvalida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "   "
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Descricao);
    }

    #endregion

    #region Data Edge Cases

    [Fact]
    public void Data_ComHoje_DeveSerValida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Data);
    }

    [Fact]
    public void Data_ComAmanha_DeveSerValida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today.AddDays(1),
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Data);
    }

    [Fact]
    public void Data_ComAnoPassado_DeveSerValida()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today.AddYears(-1),
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Data);
    }

    #endregion

    #region Tipo Edge Cases

    [Theory]
    [InlineData(TipoLancamento.Credito)]
    [InlineData(TipoLancamento.Debito)]
    public void Tipo_ComValoresValidos_DeveSerValido(TipoLancamento tipo)
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = tipo,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Tipo);
    }

    [Fact]
    public void Tipo_ComValorInvalidoDoEnum_DeveSerInvalido()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = (TipoLancamento)99,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tipo);
    }

    #endregion
}

