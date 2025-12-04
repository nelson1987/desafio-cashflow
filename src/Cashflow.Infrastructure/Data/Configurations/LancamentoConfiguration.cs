using Cashflow.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cashflow.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração do Entity Framework para a entidade Lançamento
/// </summary>
public class LancamentoConfiguration : IEntityTypeConfiguration<LancamentoEntity>
{
    public void Configure(EntityTypeBuilder<LancamentoEntity> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.Valor)
            .HasColumnName("valor")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Tipo)
            .HasColumnName("tipo")
            .IsRequired();

        builder.Property(e => e.Data)
            .HasColumnName("data")
            .IsRequired();

        builder.Property(e => e.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Índices
        builder.HasIndex(e => e.Data)
            .HasDatabaseName("idx_lancamentos_data");

        builder.HasIndex(e => e.Tipo)
            .HasDatabaseName("idx_lancamentos_tipo");

        builder.HasIndex(e => new { e.Data, e.Tipo })
            .HasDatabaseName("idx_lancamentos_data_tipo");
    }
}

