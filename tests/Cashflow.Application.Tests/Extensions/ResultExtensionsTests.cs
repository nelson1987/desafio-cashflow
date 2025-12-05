using Cashflow.Application.DTOs;
using Shouldly;
using Xunit;

namespace Cashflow.Application.Tests.Extensions;

public class ResultExtensionsTests
{
    #region Result (non-generic)

    [Fact]
    public void Success_DeveRetornarResultadoComSucesso()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_DeveRetornarResultadoComErro()
    {
        // Act
        var result = Result.Failure("Erro de teste");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Erro de teste");
    }

    [Fact]
    public void Failure_ComListaDeErros_DeveRetornarTodosOsErros()
    {
        // Arrange
        var erros = new[] { "Erro 1", "Erro 2", "Erro 3" };

        // Act
        var result = Result.Failure(erros);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldBe(erros);
        result.Error.ShouldBe("Erro 1");
    }

    #endregion

    #region Result<T> (generic)

    [Fact]
    public void Success_ComValor_DeveRetornarResultadoComValor()
    {
        // Arrange
        var valor = new TestDto { Id = 1, Nome = "Teste" };

        // Act
        var result = Result<TestDto>.Success(valor);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(valor);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_Generico_DeveRetornarResultadoSemValor()
    {
        // Act
        var result = Result<TestDto>.Failure("Erro de teste");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Error.ShouldBe("Erro de teste");
        result.Errors.ShouldContain("Erro de teste");
    }

    [Fact]
    public void Failure_Generico_ComListaDeErros_DeveRetornarTodosOsErros()
    {
        // Arrange
        var erros = new[] { "Erro 1", "Erro 2" };

        // Act
        var result = Result<TestDto>.Failure(erros);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldBe(erros);
    }

    [Fact]
    public void Success_ComValorNulo_DeveRetornarResultadoComValorNulo()
    {
        // Act
        var result = Result<TestDto?>.Success(null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Failure_ComErroVazio_DeveRetornarErroVazio()
    {
        // Act
        var result = Result.Failure("");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("");
    }

    [Fact]
    public void Failure_ComArrayVazio_DeveRetornarSemErros()
    {
        // Act
        var result = Result.Failure(Array.Empty<string>());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    private class TestDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}

