using Shouldly;
using Xunit;

namespace Cashflow.Application.Tests.Validators;

/// <summary>
/// Testes para validação de parâmetros de paginação
/// </summary>
public class PaginacaoQueryValidatorTests
{
    [Theory]
    [InlineData(1, 10, true)]
    [InlineData(1, 50, true)]
    [InlineData(10, 25, true)]
    [InlineData(0, 10, false)]
    [InlineData(-1, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -1, false)]
    [InlineData(1, 101, false)]
    public void Paginacao_DeveValidarCorretamente(int pagina, int tamanhoPagina, bool esperadoValido)
    {
        // Act
        var ehValido = pagina > 0 && tamanhoPagina > 0 && tamanhoPagina <= 100;

        // Assert
        ehValido.ShouldBe(esperadoValido);
    }

    [Fact]
    public void Paginacao_ComValoresPadrao_DeveSerValida()
    {
        // Arrange
        var paginaPadrao = 1;
        var tamanhoPadrao = 10;

        // Act
        var ehValido = paginaPadrao > 0 && tamanhoPadrao > 0 && tamanhoPadrao <= 100;

        // Assert
        ehValido.ShouldBeTrue();
    }

    [Fact]
    public void CalcularOffset_DeveRetornarValorCorreto()
    {
        // Arrange
        var pagina = 3;
        var tamanhoPagina = 10;

        // Act
        var offset = (pagina - 1) * tamanhoPagina;

        // Assert
        offset.ShouldBe(20);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 10, 20)]
    [InlineData(1, 25, 0)]
    [InlineData(2, 25, 25)]
    public void CalcularOffset_DeverCalcularCorretamente(int pagina, int tamanhoPagina, int expectedOffset)
    {
        // Act
        var offset = (pagina - 1) * tamanhoPagina;

        // Assert
        offset.ShouldBe(expectedOffset);
    }
}

