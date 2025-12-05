using Cashflow.Application.DTOs;
using Shouldly;
using Xunit;

namespace Cashflow.Application.Tests.DTOs;

public class LancamentoDtoTests
{
    [Fact]
    public void LancamentoResponse_DeveSerCriadoComValoresCorretos()
    {
        // Arrange & Act
        var response = new LancamentoResponse
        {
            Id = Guid.NewGuid(),
            Valor = 100.50m,
            Tipo = "Credito",
            Data = DateTime.UtcNow,
            Descricao = "Teste"
        };

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.Valor.ShouldBe(100.50m);
        response.Tipo.ShouldBe("Credito");
        response.Descricao.ShouldBe("Teste");
    }

    [Fact]
    public void LancamentoResponse_FromDomain_DeveConverterCorretamente()
    {
        // Arrange
        var lancamento = new Lancamento(150m, TipoLancamento.Debito, DateTime.UtcNow, "Pagamento");

        // Act
        var response = LancamentoResponse.FromDomain(lancamento);

        // Assert
        response.Id.ShouldBe(lancamento.Id);
        response.Valor.ShouldBe(150m);
        response.Tipo.ShouldBe("Debito");
        response.Descricao.ShouldBe("Pagamento");
    }

    [Fact]
    public void CriarLancamentoRequest_DeveSerCriadoComValoresCorretos()
    {
        // Arrange & Act
        var request = new CriarLancamentoRequest
        {
            Valor = 250.00m,
            Tipo = TipoLancamento.Debito,
            Data = DateTime.UtcNow,
            Descricao = "Pagamento de fornecedor"
        };

        // Assert
        request.Valor.ShouldBe(250.00m);
        request.Tipo.ShouldBe(TipoLancamento.Debito);
        request.Descricao.ShouldBe("Pagamento de fornecedor");
    }

    [Fact]
    public void LancamentosListResponse_DeveConterItensEPaginacao()
    {
        // Arrange
        var items = new List<LancamentoResponse>
        {
            new() { Id = Guid.NewGuid(), Valor = 100m, Tipo = "Credito", Data = DateTime.UtcNow, Descricao = "Item 1" },
            new() { Id = Guid.NewGuid(), Valor = 200m, Tipo = "Debito", Data = DateTime.UtcNow, Descricao = "Item 2" }
        };

        // Act
        var response = new LancamentosListResponse
        {
            Items = items,
            TotalItems = 10,
            Pagina = 1,
            TamanhoPagina = 10
        };

        // Assert
        response.Items.Count().ShouldBe(2);
        response.TotalItems.ShouldBe(10);
        response.Pagina.ShouldBe(1);
        response.TamanhoPagina.ShouldBe(10);
        response.TotalPaginas.ShouldBe(1);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(95, 10, 10)]
    [InlineData(10, 10, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(11, 10, 2)]
    public void LancamentosListResponse_DeveCalcularTotalPaginasCorretamente(int totalItems, int tamanhoPagina, int expectedTotalPaginas)
    {
        // Act
        var response = new LancamentosListResponse
        {
            Items = [],
            TotalItems = totalItems,
            Pagina = 1,
            TamanhoPagina = tamanhoPagina
        };

        // Assert
        response.TotalPaginas.ShouldBe(expectedTotalPaginas);
    }

    [Fact]
    public void LancamentosListResponse_TemProximaPagina_DeveRetornarTrue_QuandoNaoEstaNaUltimaPagina()
    {
        // Arrange
        var response = new LancamentosListResponse
        {
            Items = [],
            TotalItems = 100,
            Pagina = 1,
            TamanhoPagina = 10
        };

        // Assert
        response.TemProximaPagina.ShouldBeTrue();
    }

    [Fact]
    public void LancamentosListResponse_TemProximaPagina_DeveRetornarFalse_QuandoEstaNaUltimaPagina()
    {
        // Arrange
        var response = new LancamentosListResponse
        {
            Items = [],
            TotalItems = 100,
            Pagina = 10,
            TamanhoPagina = 10
        };

        // Assert
        response.TemProximaPagina.ShouldBeFalse();
    }

    [Fact]
    public void LancamentosListResponse_TemPaginaAnterior_DeveRetornarFalse_QuandoNaPrimeiraPagina()
    {
        // Arrange
        var response = new LancamentosListResponse
        {
            Items = [],
            TotalItems = 100,
            Pagina = 1,
            TamanhoPagina = 10
        };

        // Assert
        response.TemPaginaAnterior.ShouldBeFalse();
    }

    [Fact]
    public void LancamentosListResponse_TemPaginaAnterior_DeveRetornarTrue_QuandoNaoNaPrimeiraPagina()
    {
        // Arrange
        var response = new LancamentosListResponse
        {
            Items = [],
            TotalItems = 100,
            Pagina = 5,
            TamanhoPagina = 10
        };

        // Assert
        response.TemPaginaAnterior.ShouldBeTrue();
    }
}
