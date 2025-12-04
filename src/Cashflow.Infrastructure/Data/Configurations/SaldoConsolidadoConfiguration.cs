using Cashflow.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashflow.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração do Entity Framework para a entidade Saldo Consolidado
/// </summary>
public class SaldoConsolidadoConfiguration : IEntityTypeConfiguration<SaldoConsolidadoEntity>
{
    public void Configure(EntityTypeBuilder<SaldoConsolidadoEntity> builder)
    {
        builder.ToTable("saldos_consolidados");

        builder.HasKey(e => e.Data);

        builder.Property(e => e.Data)
            .HasColumnName("data")
            .HasColumnType("date");

        builder.Property(e => e.TotalCreditos)
            .HasColumnName("total_creditos")
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TotalDebitos)
            .HasColumnName("total_debitos")
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.Saldo)
            .HasColumnName("saldo")
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.QuantidadeLancamentos)
            .HasColumnName("quantidade_lancamentos")
            .HasDefaultValue(0);

        builder.Property(e => e.ProcessadoEm)
            .HasColumnName("processado_em")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}


