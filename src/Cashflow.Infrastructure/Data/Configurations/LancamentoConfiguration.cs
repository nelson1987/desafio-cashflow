using Cashflow.Infrastructure.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static Cashflow.DomainConstants;
using static Cashflow.Infrastructure.InfrastructureConstants;

namespace Cashflow.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração do Entity Framework para a entidade Lançamento
/// </summary>
public class LancamentoConfiguration : IEntityTypeConfiguration<LancamentoEntity>
{
    public void Configure(EntityTypeBuilder<LancamentoEntity> builder)
    {
        builder.ToTable(Tables.Lancamentos);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName(Columns.Id)
            .HasDefaultValueSql(SqlDefaults.UuidGenerateV4);

        builder.Property(e => e.Valor)
            .HasColumnName(Columns.Valor)
            .HasPrecision(ValoresMonetarios.Precisao, ValoresMonetarios.Escala)
            .IsRequired();

        builder.Property(e => e.Tipo)
            .HasColumnName(Columns.Tipo)
            .IsRequired();

        builder.Property(e => e.Data)
            .HasColumnName(Columns.Data)
            .IsRequired();

        builder.Property(e => e.Descricao)
            .HasColumnName(Columns.Descricao)
            .HasMaxLength(LancamentoLimites.DescricaoMaxLength)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName(Columns.CreatedAt)
            .HasDefaultValueSql(SqlDefaults.CurrentTimestamp);

        builder.Property(e => e.UpdatedAt)
            .HasColumnName(Columns.UpdatedAt)
            .HasDefaultValueSql(SqlDefaults.CurrentTimestamp);

        // Índices
        builder.HasIndex(e => e.Data)
            .HasDatabaseName(Indexes.LancamentosData);

        builder.HasIndex(e => e.Tipo)
            .HasDatabaseName(Indexes.LancamentosTipo);

        builder.HasIndex(e => new { e.Data, e.Tipo })
            .HasDatabaseName(Indexes.LancamentosDataTipo);
    }
}