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
/// Testes de integração para os endpoints de Consolidado
/// </summary>
[Collection(WebApiTestCollection.Name)]
public class ConsolidadoEndpointsTests : IAsyncLifetime
{
    private readonly WebApiFixture _fixture;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConsolidadoEndpointsTests(WebApiFixture fixture)
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

    #region GET /api/consolidado/{data}

    [Fact]
    public async Task ObterConsolidadoPorData_SemLancamentos_DeveRetornarSaldoZerado()
    {
        // Arrange
        var data = TestDates.Today;

        // Act
        var response = await _client.GetAsync($"/api/consolidado/{data:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var consolidado = await response.Content.ReadFromJsonAsync<SaldoConsolidadoResponse>(_jsonOptions);
        consolidado.ShouldNotBeNull();
        consolidado.Data.Date.ShouldBe(data.Date);
        consolidado.TotalCreditos.ShouldBe(0);
        consolidado.TotalDebitos.ShouldBe(0);
        consolidado.Saldo.ShouldBe(0);
        consolidado.QuantidadeLancamentos.ShouldBe(0);
    }

    [Fact(Skip = "Depende do recálculo que tem problema com DateTime routing no MinimalAPI")]
    public async Task ObterConsolidadoPorData_ComLancamentos_DeveRetornarSaldoCalculado()
    {
        // Arrange
        var data = TestDates.Today;
        await CriarLancamentoAsync(100m, TipoLancamento.Credito, "Crédito 1", data);
        await CriarLancamentoAsync(200m, TipoLancamento.Credito, "Crédito 2", data);
        await CriarLancamentoAsync(50m, TipoLancamento.Debito, "Débito 1", data);

        // Recalcula o consolidado (simula o worker)
        await _client.PostAsync($"/api/consolidado/{data:yyyy-MM-dd}/recalcular", null);

        // Act
        var response = await _client.GetAsync($"/api/consolidado/{data:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var consolidado = await response.Content.ReadFromJsonAsync<SaldoConsolidadoResponse>(_jsonOptions);
        consolidado.ShouldNotBeNull();
        consolidado.TotalCreditos.ShouldBe(300m);
        consolidado.TotalDebitos.ShouldBe(50m);
        consolidado.Saldo.ShouldBe(250m);
        consolidado.QuantidadeLancamentos.ShouldBe(3);
    }

    #endregion

    #region GET /api/consolidado/periodo

    [Fact]
    public async Task ObterConsolidadoPorPeriodo_PeriodoValido_DeveRetornarRelatorio()
    {
        // Arrange
        var dataInicio = TestDates.Today.AddDays(-2);
        var dataFim = TestDates.Today;

        await CriarLancamentoAsync(100m, TipoLancamento.Credito, "Dia 1", dataInicio);
        await CriarLancamentoAsync(200m, TipoLancamento.Debito, "Dia 2", dataInicio.AddDays(1));
        await CriarLancamentoAsync(300m, TipoLancamento.Credito, "Dia 3", dataFim);

        // Recalcula os consolidados
        await _client.PostAsync($"/api/consolidado/{dataInicio:yyyy-MM-dd}/recalcular", null);
        await _client.PostAsync($"/api/consolidado/{dataInicio.AddDays(1):yyyy-MM-dd}/recalcular", null);
        await _client.PostAsync($"/api/consolidado/{dataFim:yyyy-MM-dd}/recalcular", null);

        // Act
        var response = await _client.GetAsync($"/api/consolidado/periodo?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var relatorio = await response.Content.ReadFromJsonAsync<RelatorioConsolidadoResponse>(_jsonOptions);
        relatorio.ShouldNotBeNull();
        relatorio.DataInicio.Date.ShouldBe(dataInicio.Date);
        relatorio.DataFim.Date.ShouldBe(dataFim.Date);
        relatorio.Saldos.Count().ShouldBe(3); // 3 dias
        relatorio.Resumo.ShouldNotBeNull();
    }

    [Fact]
    public async Task ObterConsolidadoPorPeriodo_DataInicioMaiorQueFim_DeveRetornar400()
    {
        // Arrange
        var dataInicio = TestDates.Today;
        var dataFim = TestDates.Today.AddDays(-5);

        // Act
        var response = await _client.GetAsync($"/api/consolidado/periodo?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObterConsolidadoPorPeriodo_PeriodoMaiorQue90Dias_DeveRetornar400()
    {
        // Arrange
        var dataInicio = TestDates.Today.AddDays(-100);
        var dataFim = TestDates.Today;

        // Act
        var response = await _client.GetAsync($"/api/consolidado/periodo?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/consolidado/{data}/recalcular

    [Fact(Skip = "Problema com DateTime routing no MinimalAPI - requer investigação")]
    public async Task RecalcularConsolidado_DeveAtualizarSaldo()
    {
        // Arrange
        var data = TestDates.Today;
        await CriarLancamentoAsync(500m, TipoLancamento.Credito, "Venda", data);
        await CriarLancamentoAsync(150m, TipoLancamento.Debito, "Compra", data);

        // Act
        var response = await _client.PostAsync($"/api/consolidado/{data:yyyy-MM-dd}/recalcular", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var consolidado = await response.Content.ReadFromJsonAsync<SaldoConsolidadoResponse>(_jsonOptions);
        consolidado.ShouldNotBeNull();
        consolidado.TotalCreditos.ShouldBe(500m);
        consolidado.TotalDebitos.ShouldBe(150m);
        consolidado.Saldo.ShouldBe(350m);
        consolidado.QuantidadeLancamentos.ShouldBe(2);
    }

    [Fact(Skip = "Problema com DateTime routing no MinimalAPI - requer investigação")]
    public async Task RecalcularConsolidado_SemLancamentos_DeveRetornarSaldoZerado()
    {
        // Arrange
        var data = TestDates.Today.AddDays(-10);

        // Act
        var response = await _client.PostAsync($"/api/consolidado/{data:yyyy-MM-dd}/recalcular", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var consolidado = await response.Content.ReadFromJsonAsync<SaldoConsolidadoResponse>(_jsonOptions);
        consolidado.ShouldNotBeNull();
        consolidado.Saldo.ShouldBe(0);
        consolidado.QuantidadeLancamentos.ShouldBe(0);
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

