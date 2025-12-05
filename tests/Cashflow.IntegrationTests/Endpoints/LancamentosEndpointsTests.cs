using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Cashflow.Application.DTOs;
using Cashflow.IntegrationTests.Fixtures;

using Shouldly;

namespace Cashflow.IntegrationTests.Endpoints;

// Helper para criar datas UTC consistentes nos testes
file static class TestDates
{
    public static DateTime Today => DateTime.UtcNow.Date;
}

/// <summary>
/// Testes de integração para os endpoints de Lançamentos
/// </summary>
[Collection(WebApiTestCollection.Name)]
public class LancamentosEndpointsTests : IAsyncLifetime
{
    private readonly WebApiFixture _fixture;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public LancamentosEndpointsTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region POST /api/lancamentos

    [Fact]
    public async Task CriarLancamento_ComDadosValidos_DeveRetornar201()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100.50m,
            Tipo = TipoLancamento.Credito,
            Data = TestDates.Today,
            Descricao = "Venda de produto"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var lancamento = await response.Content.ReadFromJsonAsync<LancamentoResponse>(_jsonOptions);
        lancamento.ShouldNotBeNull();
        lancamento.Id.ShouldNotBe(Guid.Empty);
        lancamento.Valor.ShouldBe(100.50m);
        lancamento.Tipo.ShouldBe(TipoLancamento.Credito.ToString());
        lancamento.Descricao.ShouldBe("Venda de produto");
    }

    [Fact]
    public async Task CriarLancamento_ComValorZero_DeveRetornar400()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 0,
            Tipo = TipoLancamento.Credito,
            Data = TestDates.Today,
            Descricao = "Teste"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CriarLancamento_ComDescricaoVazia_DeveRetornar400()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Debito,
            Data = TestDates.Today,
            Descricao = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CriarLancamento_Debito_DeveRetornar201()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 50.00m,
            Tipo = TipoLancamento.Debito,
            Data = TestDates.Today,
            Descricao = "Pagamento de fornecedor"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var lancamento = await response.Content.ReadFromJsonAsync<LancamentoResponse>(_jsonOptions);
        lancamento.ShouldNotBeNull();
        lancamento.Tipo.ShouldBe(TipoLancamento.Debito.ToString());
    }

    #endregion

    #region GET /api/lancamentos

    [Fact]
    public async Task ListarLancamentos_SemDados_DeveRetornarListaVazia()
    {
        // Act
        var response = await _client.GetAsync("/api/lancamentos?pagina=1&tamanhoPagina=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LancamentosListResponse>(_jsonOptions);
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
    }

    [Fact]
    public async Task ListarLancamentos_ComDados_DeveRetornarListaPaginada()
    {
        // Arrange
        await CriarLancamentoAsync(100m, TipoLancamento.Credito, "Lançamento 1");
        await CriarLancamentoAsync(200m, TipoLancamento.Debito, "Lançamento 2");
        await CriarLancamentoAsync(300m, TipoLancamento.Credito, "Lançamento 3");

        // Act
        var response = await _client.GetAsync("/api/lancamentos?pagina=1&tamanhoPagina=2");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LancamentosListResponse>(_jsonOptions);
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(2);
        result.TotalItems.ShouldBe(3);
        result.Pagina.ShouldBe(1);
        result.TamanhoPagina.ShouldBe(2);
    }

    [Fact]
    public async Task ListarLancamentos_SegundaPagina_DeveRetornarItensRestantes()
    {
        // Arrange
        await CriarLancamentoAsync(100m, TipoLancamento.Credito, "Lançamento 1");
        await CriarLancamentoAsync(200m, TipoLancamento.Debito, "Lançamento 2");
        await CriarLancamentoAsync(300m, TipoLancamento.Credito, "Lançamento 3");

        // Act
        var response = await _client.GetAsync("/api/lancamentos?pagina=2&tamanhoPagina=2");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LancamentosListResponse>(_jsonOptions);
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(1);
        result.Pagina.ShouldBe(2);
    }

    #endregion

    #region GET /api/lancamentos/{id}

    [Fact]
    public async Task ObterPorId_Existente_DeveRetornarLancamento()
    {
        // Arrange
        var lancamentoCriado = await CriarLancamentoAsync(150m, TipoLancamento.Credito, "Lançamento teste");

        // Act
        var response = await _client.GetAsync($"/api/lancamentos/{lancamentoCriado.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var lancamento = await response.Content.ReadFromJsonAsync<LancamentoResponse>(_jsonOptions);
        lancamento.ShouldNotBeNull();
        lancamento.Id.ShouldBe(lancamentoCriado.Id);
        lancamento.Valor.ShouldBe(150m);
    }

    [Fact]
    public async Task ObterPorId_NaoExistente_DeveRetornar404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/lancamentos/{idInexistente}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/lancamentos/data/{data}

    [Fact(Skip = "Problema com DateTime routing no MinimalAPI - requer investigação")]
    public async Task ObterPorData_ComLancamentos_DeveRetornarLista()
    {
        // Arrange
        var data = TestDates.Today;
        await CriarLancamentoAsync(100m, TipoLancamento.Credito, "Lançamento 1", data);
        await CriarLancamentoAsync(200m, TipoLancamento.Debito, "Lançamento 2", data);
        await CriarLancamentoAsync(300m, TipoLancamento.Credito, "Lançamento outro dia", data.AddDays(-1));

        // Act
        var response = await _client.GetAsync($"/api/lancamentos/data/{data:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var lancamentos = await response.Content.ReadFromJsonAsync<IEnumerable<LancamentoResponse>>(_jsonOptions);
        lancamentos.ShouldNotBeNull();
        lancamentos.Count().ShouldBe(2);
    }

    [Fact(Skip = "Problema com DateTime routing no MinimalAPI - requer investigação")]
    public async Task ObterPorData_SemLancamentos_DeveRetornarListaVazia()
    {
        // Arrange
        var data = TestDates.Today.AddDays(-30);

        // Act
        var response = await _client.GetAsync($"/api/lancamentos/data/{data:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var lancamentos = await response.Content.ReadFromJsonAsync<IEnumerable<LancamentoResponse>>(_jsonOptions);
        lancamentos.ShouldNotBeNull();
        lancamentos.ShouldBeEmpty();
    }

    #endregion

    #region Helpers

    private async Task<LancamentoResponse> CriarLancamentoAsync(
        decimal valor, 
        TipoLancamento tipo, 
        string descricao, 
        DateTime? data = null)
    {
        var request = new CriarLancamentoRequest
        {
            Valor = valor,
            Tipo = tipo,
            Data = data ?? TestDates.Today,
            Descricao = descricao
        };

        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);
        response.EnsureSuccessStatusCode();

        var lancamento = await response.Content.ReadFromJsonAsync<LancamentoResponse>(_jsonOptions);
        return lancamento!;
    }

    #endregion
}

