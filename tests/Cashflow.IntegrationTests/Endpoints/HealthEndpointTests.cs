using System.Net;

using Cashflow.IntegrationTests.Fixtures;

using Shouldly;

namespace Cashflow.IntegrationTests.Endpoints;

/// <summary>
/// Testes de integração para o endpoint de Health Check
/// </summary>
[Collection(WebApiTestCollection.Name)]
public class HealthEndpointTests
{
    private readonly WebApiFixture _fixture;
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task Health_QuandoAplicacaoSaudavel_DeveRetornar200()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_DeveRetornarContentTypeCorreto()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        // O health check padrão do ASP.NET retorna text/plain com "Healthy" ou "Unhealthy"
        var contentType = response.Content.Headers.ContentType?.MediaType;
        (contentType == "application/json" || contentType == "text/plain").ShouldBeTrue();
    }
}