using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Repositories;
using Cashflow.IntegrationTests.Fixtures;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Cashflow.IntegrationTests.Repositories;

// Helper para criar datas UTC consistentes nos testes
file static class SaldoTestDates
{
    public static DateTime Today => DateTime.UtcNow.Date;
    public static DateTime UtcDate(int year, int month, int day) => 
        new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>
/// Testes de integração para SaldoConsolidadoRepository
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public class SaldoConsolidadoRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private CashflowDbContext _context = null!;
    private SaldoConsolidadoRepository _repository = null!;

    public SaldoConsolidadoRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _repository = new SaldoConsolidadoRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Limpa os dados após cada teste
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM cashflow.saldos_consolidados");
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SalvarAsync_DevePersistirSaldoConsolidado()
    {
        // Arrange
        var saldo = new SaldoDiario(
            data: SaldoTestDates.Today,
            totalCreditos: 1000m,
            totalDebitos: 400m,
            quantidadeLancamentos: 5);

        // Act
        await _repository.SalvarAsync(saldo);

        // Assert
        var resultado = await _repository.ObterPorDataAsync(SaldoTestDates.Today);
        resultado.ShouldNotBeNull();
        resultado.TotalCreditos.ShouldBe(1000m);
        resultado.TotalDebitos.ShouldBe(400m);
        resultado.Saldo.ShouldBe(600m);
        resultado.QuantidadeLancamentos.ShouldBe(5);
    }

    [Fact]
    public async Task SalvarAsync_DeveAtualizarSaldoExistente()
    {
        // Arrange
        var saldoOriginal = new SaldoDiario(SaldoTestDates.Today, 500m, 200m, 3);
        await _repository.SalvarAsync(saldoOriginal);

        var saldoAtualizado = new SaldoDiario(SaldoTestDates.Today, 800m, 300m, 5);

        // Act
        await _repository.SalvarAsync(saldoAtualizado);

        // Assert
        var resultado = await _repository.ObterPorDataAsync(SaldoTestDates.Today);
        resultado.ShouldNotBeNull();
        resultado.TotalCreditos.ShouldBe(800m);
        resultado.TotalDebitos.ShouldBe(300m);
        resultado.Saldo.ShouldBe(500m);
        resultado.QuantidadeLancamentos.ShouldBe(5);
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarNullParaDataInexistente()
    {
        // Act
        var resultado = await _repository.ObterPorDataAsync(new DateTime(2099, 12, 31));

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarSaldosDoPeriodo()
    {
        // Arrange
        var data1 = new DateTime(2024, 6, 1);
        var data2 = new DateTime(2024, 6, 15);
        var data3 = new DateTime(2024, 6, 30);
        var dataForaDoPeriodo = new DateTime(2024, 7, 15);

        await _repository.SalvarAsync(new SaldoDiario(data1, 1000m, 500m, 5));
        await _repository.SalvarAsync(new SaldoDiario(data2, 800m, 300m, 4));
        await _repository.SalvarAsync(new SaldoDiario(data3, 600m, 200m, 3));
        await _repository.SalvarAsync(new SaldoDiario(dataForaDoPeriodo, 400m, 100m, 2));

        // Act
        var resultado = (await _repository.ObterPorPeriodoAsync(data1, data3)).ToList();

        // Assert
        resultado.Count.ShouldBe(3);
        resultado.ShouldAllBe(s => s.Data >= data1 && s.Data <= data3);
    }

    [Fact]
    public async Task ObterPorPeriodo_DevePermitirCalcularSaldoAcumulado()
    {
        // Arrange
        var dia1 = new DateTime(2024, 6, 1);
        var dia2 = new DateTime(2024, 6, 2);
        var dia3 = new DateTime(2024, 6, 3);

        await _repository.SalvarAsync(new SaldoDiario(dia1, 1000m, 300m, 3)); // Saldo: 700
        await _repository.SalvarAsync(new SaldoDiario(dia2, 500m, 200m, 2));  // Saldo: 300
        await _repository.SalvarAsync(new SaldoDiario(dia3, 800m, 400m, 4));  // Saldo: 400

        // Act - Busca todos os saldos até cada dia
        var saldosAteDia1 = await _repository.ObterPorPeriodoAsync(DateTime.MinValue.AddYears(1), dia1);
        var saldosAteDia2 = await _repository.ObterPorPeriodoAsync(DateTime.MinValue.AddYears(1), dia2);
        var saldosAteDia3 = await _repository.ObterPorPeriodoAsync(DateTime.MinValue.AddYears(1), dia3);

        // Calcula saldos acumulados
        var saldoAcumuladoDia1 = saldosAteDia1.Sum(s => s.Saldo);
        var saldoAcumuladoDia2 = saldosAteDia2.Sum(s => s.Saldo);
        var saldoAcumuladoDia3 = saldosAteDia3.Sum(s => s.Saldo);

        // Assert
        saldoAcumuladoDia1.ShouldBe(700m);  // 700
        saldoAcumuladoDia2.ShouldBe(1000m); // 700 + 300
        saldoAcumuladoDia3.ShouldBe(1400m); // 700 + 300 + 400
    }

    [Fact]
    public async Task ObterPorPeriodo_DeveRetornarVazioSemDados()
    {
        // Act
        var saldos = await _repository.ObterPorPeriodoAsync(
            new DateTime(2099, 1, 1),
            new DateTime(2099, 12, 31));

        // Assert
        saldos.ShouldBeEmpty();
    }

    [Fact]
    public async Task SalvarMultiplosSaldos_DeveManterIntegridadeDados()
    {
        // Arrange
        var saldos = new List<SaldoDiario>();
        var dataBase = new DateTime(2024, 1, 1);

        for (int i = 0; i < 30; i++)
        {
            saldos.Add(new SaldoDiario(
                dataBase.AddDays(i),
                1000m + (i * 100),
                400m + (i * 50),
                5 + i));
        }

        // Act
        foreach (var saldo in saldos)
        {
            await _repository.SalvarAsync(saldo);
        }

        // Assert
        var resultado = (await _repository.ObterPorPeriodoAsync(
            dataBase,
            dataBase.AddDays(29))).ToList();

        resultado.Count.ShouldBe(30);

        // Verifica o primeiro e o último
        var primeiro = resultado.First();
        primeiro.TotalCreditos.ShouldBe(1000m);
        primeiro.TotalDebitos.ShouldBe(400m);

        var ultimo = resultado.Last();
        ultimo.TotalCreditos.ShouldBe(3900m);  // 1000 + 29*100
        ultimo.TotalDebitos.ShouldBe(1850m);   // 400 + 29*50
    }
}