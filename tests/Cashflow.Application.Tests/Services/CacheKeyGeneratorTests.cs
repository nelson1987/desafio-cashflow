using Shouldly;
using Xunit;

namespace Cashflow.Application.Tests.Services;

/// <summary>
/// Testes para validar comportamentos de cache keys
/// </summary>
public class CacheKeyGeneratorTests
{
    [Fact]
    public void CacheKey_ConsolidadoPorData_DeveGerarChaveCorreta()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var expectedKey = "consolidado:2024-01-15";

        // Act
        var key = $"consolidado:{data:yyyy-MM-dd}";

        // Assert
        key.ShouldBe(expectedKey);
    }

    [Fact]
    public void CacheKey_ConsolidadoPorPeriodo_DeveGerarChaveCorreta()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 1, 1);
        var dataFim = new DateTime(2024, 1, 31);
        var expectedKey = "consolidado:periodo:2024-01-01:2024-01-31";

        // Act
        var key = $"consolidado:periodo:{dataInicio:yyyy-MM-dd}:{dataFim:yyyy-MM-dd}";

        // Assert
        key.ShouldBe(expectedKey);
    }

    [Fact]
    public void CacheKey_LancamentoPorId_DeveGerarChaveCorreta()
    {
        // Arrange
        var id = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var expectedKey = "lancamento:12345678-1234-1234-1234-123456789012";

        // Act
        var key = $"lancamento:{id}";

        // Assert
        key.ShouldBe(expectedKey);
    }

    [Theory]
    [InlineData(2024, 1, 1, "consolidado:2024-01-01")]
    [InlineData(2024, 12, 31, "consolidado:2024-12-31")]
    [InlineData(2023, 6, 15, "consolidado:2023-06-15")]
    public void CacheKey_DiferentesDatas_DeveGerarChavesCorretas(int ano, int mes, int dia, string expectedKey)
    {
        // Arrange
        var data = new DateTime(ano, mes, dia);

        // Act
        var key = $"consolidado:{data:yyyy-MM-dd}";

        // Assert
        key.ShouldBe(expectedKey);
    }
}

