using System.Text.Json;

using Cashflow.Abstractions;
using Cashflow.Infrastructure.Cache;
using Cashflow.IntegrationTests.Fixtures;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Cashflow.IntegrationTests.Cache;

/// <summary>
/// Testes de integração para RedisCacheService
/// </summary>
[Collection(RedisCollection.Name)]
public class RedisCacheServiceTests : IAsyncLifetime
{
    private readonly RedisContainerFixture _fixture;
    private IDistributedCache _distributedCache = null!;
    private ICacheService _cacheService = null!;

    public RedisCacheServiceTests(RedisContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.FlushDatabaseAsync();
        _distributedCache = _fixture.CreateDistributedCache();
        _cacheService = new RedisCacheService(
            _distributedCache,
            NullLogger<RedisCacheService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _fixture.FlushDatabaseAsync();
    }

    [Fact]
    public async Task DefinirAsync_DevePersistirValorNoRedis()
    {
        // Arrange
        var chave = "teste:definir:1";
        var valor = new TestData { Id = 1, Nome = "Teste" };

        // Act
        await _cacheService.DefinirAsync(chave, valor);

        // Assert
        var resultado = await _cacheService.ObterAsync<TestData>(chave);
        resultado.ShouldNotBeNull();
        resultado.Id.ShouldBe(1);
        resultado.Nome.ShouldBe("Teste");
    }

    [Fact]
    public async Task ObterAsync_DeveRetornarNullParaChaveInexistente()
    {
        // Act
        var resultado = await _cacheService.ObterAsync<TestData>("chave:inexistente");

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task RemoverAsync_DeveRemoverValorDoCache()
    {
        // Arrange
        var chave = "teste:remover:1";
        var valor = new TestData { Id = 1, Nome = "Para Remover" };
        await _cacheService.DefinirAsync(chave, valor);

        // Act
        await _cacheService.RemoverAsync(chave);

        // Assert
        var resultado = await _cacheService.ObterAsync<TestData>(chave);
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task ExisteAsync_DeveRetornarTrueParaChaveExistente()
    {
        // Arrange
        var chave = "teste:existe:1";
        var valor = new TestData { Id = 1, Nome = "Existe" };
        await _cacheService.DefinirAsync(chave, valor);

        // Act
        var existe = await _cacheService.ExisteAsync(chave);

        // Assert
        existe.ShouldBeTrue();
    }

    [Fact]
    public async Task ExisteAsync_DeveRetornarFalseParaChaveInexistente()
    {
        // Act
        var existe = await _cacheService.ExisteAsync("chave:nao:existe");

        // Assert
        existe.ShouldBeFalse();
    }

    [Fact]
    public async Task ObterOuDefinirAsync_DeveRetornarValorDoCache()
    {
        // Arrange
        var chave = "teste:obter-ou-definir:1";
        var valorOriginal = new TestData { Id = 1, Nome = "Original" };
        await _cacheService.DefinirAsync(chave, valorOriginal);

        var factoryChamada = false;

        // Act
        var resultado = await _cacheService.ObterOuDefinirAsync(
            chave,
            () =>
            {
                factoryChamada = true;
                return Task.FromResult<TestData?>(new TestData { Id = 2, Nome = "Factory" });
            });

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Id.ShouldBe(1);
        resultado.Nome.ShouldBe("Original");
        factoryChamada.ShouldBeFalse();
    }

    [Fact]
    public async Task ObterOuDefinirAsync_DeveChamarFactoryQuandoCacheVazio()
    {
        // Arrange
        var chave = "teste:obter-ou-definir:2";
        var factoryChamada = false;

        // Act
        var resultado = await _cacheService.ObterOuDefinirAsync(
            chave,
            () =>
            {
                factoryChamada = true;
                return Task.FromResult<TestData?>(new TestData { Id = 3, Nome = "Factory" });
            });

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Id.ShouldBe(3);
        resultado.Nome.ShouldBe("Factory");
        factoryChamada.ShouldBeTrue();

        // Verifica se foi salvo no cache
        var valorSalvo = await _cacheService.ObterAsync<TestData>(chave);
        valorSalvo.ShouldNotBeNull();
        valorSalvo.Nome.ShouldBe("Factory");
    }

    [Fact]
    public async Task DefinirAsync_ComTTL_DeveExpirarAposTempoDefinido()
    {
        // Arrange
        var chave = "teste:ttl:1";
        var valor = new TestData { Id = 1, Nome = "Com TTL" };
        var ttl = TimeSpan.FromSeconds(2);

        // Act
        await _cacheService.DefinirAsync(chave, valor, ttl);

        // Assert - deve existir imediatamente
        var resultadoImediato = await _cacheService.ObterAsync<TestData>(chave);
        resultadoImediato.ShouldNotBeNull();

        // Aguarda expiração
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert - deve ter expirado
        var resultadoAposExpiracao = await _cacheService.ObterAsync<TestData>(chave);
        resultadoAposExpiracao.ShouldBeNull();
    }

    [Fact]
    public async Task Cache_DeveSerializarObjetosComplexos()
    {
        // Arrange
        var chave = "teste:objeto-complexo:1";
        var saldoDiario = new SaldoDiario(
            data: DateTime.Today,
            totalCreditos: 5000m,
            totalDebitos: 2000m,
            quantidadeLancamentos: 15);

        // Act
        await _cacheService.DefinirAsync(chave, saldoDiario);
        var resultado = await _cacheService.ObterAsync<SaldoDiario>(chave);

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Data.ShouldBe(DateTime.Today);
        resultado.TotalCreditos.ShouldBe(5000m);
        resultado.TotalDebitos.ShouldBe(2000m);
        resultado.Saldo.ShouldBe(3000m);
        resultado.QuantidadeLancamentos.ShouldBe(15);
    }

    [Fact]
    public async Task Cache_DeveSuportarMultiplasChavesConcorrentes()
    {
        // Arrange
        var tarefas = new List<Task>();
        var quantidadeChaves = 100;

        // Act
        for (int i = 0; i < quantidadeChaves; i++)
        {
            var indice = i;
            tarefas.Add(Task.Run(async () =>
            {
                var chave = $"teste:concorrente:{indice}";
                var valor = new TestData { Id = indice, Nome = $"Valor {indice}" };
                await _cacheService.DefinirAsync(chave, valor);
            }));
        }

        await Task.WhenAll(tarefas);

        // Assert
        for (int i = 0; i < quantidadeChaves; i++)
        {
            var chave = $"teste:concorrente:{i}";
            var resultado = await _cacheService.ObterAsync<TestData>(chave);
            resultado.ShouldNotBeNull();
            resultado.Id.ShouldBe(i);
        }
    }

    [Fact]
    public void CacheKeys_DeveGerarChavesCorretas()
    {
        // Arrange
        var data = new DateTime(2024, 6, 15);
        var id = Guid.NewGuid();

        // Act & Assert
        CacheKeys.SaldoConsolidado(data).ShouldBe("saldo:consolidado:2024-06-15");
        CacheKeys.Lancamento(id).ShouldBe($"lancamento:{id}");
        CacheKeys.LancamentosDoDia(data).ShouldBe("lancamentos:dia:2024-06-15");
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}