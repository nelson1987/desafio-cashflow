using Cashflow.Infrastructure.Data.Entities;

using Microsoft.EntityFrameworkCore;

namespace Cashflow.Infrastructure.Data;

/// <summary>
/// DbContext do Entity Framework para o Cashflow
/// </summary>
public class CashflowDbContext : DbContext
{
    public CashflowDbContext(DbContextOptions<CashflowDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Tabela de lançamentos
    /// </summary>
    public DbSet<LancamentoEntity> Lancamentos => Set<LancamentoEntity>();

    /// <summary>
    /// Tabela de saldos consolidados
    /// </summary>
    public DbSet<SaldoConsolidadoEntity> SaldosConsolidados => Set<SaldoConsolidadoEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Define o schema padrão
        modelBuilder.HasDefaultSchema("cashflow");

        // Aplica todas as configurações do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashflowDbContext).Assembly);
    }
}
