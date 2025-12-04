using Cashflow.Infrastructure.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Cashflow.DomainConstants;
using static Cashflow.Infrastructure.InfrastructureConstants;

namespace Cashflow.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração do Entity Framework para a entidade Saldo Consolidado
/// </summary>
public class SaldoConsolidadoConfiguration : IEntityTypeConfiguration<SaldoConsolidadoEntity>
{
    public void Configure(EntityTypeBuilder<SaldoConsolidadoEntity> builder)
    {
        builder.ToTable(Tables.SaldosConsolidados);

        builder.HasKey(e => e.Data);

        builder.Property(e => e.Data)
            .HasColumnName(Columns.Data)
            .HasColumnType(SqlDefaults.DateColumnType);

        builder.Property(e => e.TotalCreditos)
            .HasColumnName(Columns.TotalCreditos)
            .HasPrecision(ValoresMonetarios.Precisao, ValoresMonetarios.Escala)
            .HasDefaultValue(ValoresPadrao.Zero);

        builder.Property(e => e.TotalDebitos)
            .HasColumnName(Columns.TotalDebitos)
            .HasPrecision(ValoresMonetarios.Precisao, ValoresMonetarios.Escala)
            .HasDefaultValue(ValoresPadrao.Zero);

        builder.Property(e => e.Saldo)
            .HasColumnName(Columns.Saldo)
            .HasPrecision(ValoresMonetarios.Precisao, ValoresMonetarios.Escala)
            .HasDefaultValue(ValoresPadrao.Zero);

        builder.Property(e => e.QuantidadeLancamentos)
            .HasColumnName(Columns.QuantidadeLancamentos)
            .HasDefaultValue(ValoresPadrao.QuantidadeZero);

        builder.Property(e => e.ProcessadoEm)
            .HasColumnName(Columns.ProcessadoEm)
            .HasDefaultValueSql(SqlDefaults.CurrentTimestamp);
    }
}