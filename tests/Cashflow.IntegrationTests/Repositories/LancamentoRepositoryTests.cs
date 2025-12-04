using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Repositories;
using Cashflow.IntegrationTests.Fixtures;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Cashflow.IntegrationTests.Repositories;

/// <summary>
/// Testes de integração para LancamentoRepository
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public class LancamentoRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private CashflowDbContext _context = null!;
    private LancamentoRepository _repository = null!;

    public LancamentoRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _repository = new LancamentoRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Limpa os dados após cada teste
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM cashflow.lancamentos");
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AdicionarAsync_DevePersistirLancamento()
    {
        // Arrange
        var lancamento = new Lancamento(
            valor: 100m,
            tipo: TipoLancamento.Credito,
            data: DateTime.Today,
            descricao: "Teste de integração");

        // Act
        var resultado = await _repository.AdicionarAsync(lancamento);

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Id.ShouldBe(lancamento.Id);
        resultado.Valor.ShouldBe(100m);
        resultado.Tipo.ShouldBe(TipoLancamento.Credito);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarLancamentoExistente()
    {
        // Arrange
        var lancamento = new Lancamento(
            valor: 250m,
            tipo: TipoLancamento.Debito,
            data: DateTime.Today,
            descricao: "Lançamento para busca");

        await _repository.AdicionarAsync(lancamento);

        // Act
        var resultado = await _repository.ObterPorIdAsync(lancamento.Id);

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Id.ShouldBe(lancamento.Id);
        resultado.Valor.ShouldBe(250m);
        resultado.Descricao.ShouldBe("Lançamento para busca");
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarNullParaIdInexistente()
    {
        // Act
        var resultado = await _repository.ObterPorIdAsync(Guid.NewGuid());

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarLancamentosDoDia()
    {
        // Arrange
        var hoje = DateTime.Today;
        var ontem = hoje.AddDays(-1);

        await _repository.AdicionarAsync(new Lancamento(100m, TipoLancamento.Credito, hoje, "Hoje 1"));
        await _repository.AdicionarAsync(new Lancamento(200m, TipoLancamento.Credito, hoje, "Hoje 2"));
        await _repository.AdicionarAsync(new Lancamento(300m, TipoLancamento.Debito, ontem, "Ontem 1"));

        // Act
        var resultado = (await _repository.ObterPorDataAsync(hoje)).ToList();

        // Assert
        resultado.Count.ShouldBe(2);
        resultado.ShouldAllBe(l => l.EhDoDia(hoje));
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarLancamentosDoPeriodo()
    {
        // Arrange
        var dataInicio = new DateTime(2024, 6, 1);
        var dataFim = new DateTime(2024, 6, 30);

        await _repository.AdicionarAsync(new Lancamento(100m, TipoLancamento.Credito, new DateTime(2024, 5, 31), "Maio"));
        await _repository.AdicionarAsync(new Lancamento(200m, TipoLancamento.Credito, new DateTime(2024, 6, 15), "Junho"));
        await _repository.AdicionarAsync(new Lancamento(300m, TipoLancamento.Debito, new DateTime(2024, 6, 20), "Junho 2"));
        await _repository.AdicionarAsync(new Lancamento(400m, TipoLancamento.Credito, new DateTime(2024, 7, 1), "Julho"));

        // Act
        var resultado = (await _repository.ObterPorPeriodoAsync(dataInicio, dataFim)).ToList();

        // Assert
        resultado.Count.ShouldBe(2);
        resultado.ShouldAllBe(l => l.Data >= dataInicio && l.Data <= dataFim);
    }

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarLancamentosPaginados()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            await _repository.AdicionarAsync(new Lancamento(
                100m + i,
                TipoLancamento.Credito,
                DateTime.Today.AddDays(-i),
                $"Lançamento {i + 1}"));
        }

        // Act
        var pagina1 = (await _repository.ObterTodosAsync(1, 10)).ToList();
        var pagina2 = (await _repository.ObterTodosAsync(2, 10)).ToList();

        // Assert
        pagina1.Count.ShouldBe(10);
        pagina2.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ContarAsync_DeveRetornarQuantidadeCorreta()
    {
        // Arrange
        await _repository.AdicionarAsync(new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, "Teste 1"));
        await _repository.AdicionarAsync(new Lancamento(200m, TipoLancamento.Debito, DateTime.Today, "Teste 2"));
        await _repository.AdicionarAsync(new Lancamento(300m, TipoLancamento.Credito, DateTime.Today, "Teste 3"));

        // Act
        var quantidade = await _repository.ContarAsync();

        // Assert
        quantidade.ShouldBe(3);
    }

    [Fact]
    public async Task AdicionarMultiplosLancamentos_DeveCalcularSaldoCorretamente()
    {
        // Arrange & Act
        await _repository.AdicionarAsync(new Lancamento(1000m, TipoLancamento.Credito, DateTime.Today, "Venda 1"));
        await _repository.AdicionarAsync(new Lancamento(500m, TipoLancamento.Credito, DateTime.Today, "Venda 2"));
        await _repository.AdicionarAsync(new Lancamento(300m, TipoLancamento.Debito, DateTime.Today, "Pagamento"));
        await _repository.AdicionarAsync(new Lancamento(200m, TipoLancamento.Debito, DateTime.Today, "Compra"));

        var lancamentos = (await _repository.ObterPorDataAsync(DateTime.Today)).ToList();

        // Assert
        var totalCreditos = lancamentos.Where(l => l.Tipo == TipoLancamento.Credito).Sum(l => l.Valor);
        var totalDebitos = lancamentos.Where(l => l.Tipo == TipoLancamento.Debito).Sum(l => l.Valor);
        var saldo = totalCreditos - totalDebitos;

        totalCreditos.ShouldBe(1500m);
        totalDebitos.ShouldBe(500m);
        saldo.ShouldBe(1000m);
    }
}