namespace Cashflow.Infrastructure.Data.Entities;

/// <summary>
/// Entidade de persistência para Lançamento
/// </summary>
public class LancamentoEntity
{
    public Guid Id { get; set; }
    public decimal Valor { get; set; }
    public short Tipo { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Converte a entidade de persistência para o modelo de domínio
    /// </summary>
    public Lancamento ToDomain()
    {
        // Usa reflection para criar o lançamento sem passar pelo construtor
        // pois o domínio já foi validado quando foi persistido
        var lancamento = (Lancamento)Activator.CreateInstance(typeof(Lancamento), nonPublic: true)!;

        var idProperty = typeof(Lancamento).GetProperty(nameof(Lancamento.Id));
        var valorProperty = typeof(Lancamento).GetProperty(nameof(Lancamento.Valor));
        var tipoProperty = typeof(Lancamento).GetProperty(nameof(Lancamento.Tipo));
        var dataProperty = typeof(Lancamento).GetProperty(nameof(Lancamento.Data));
        var descricaoProperty = typeof(Lancamento).GetProperty(nameof(Lancamento.Descricao));

        idProperty?.SetValue(lancamento, Id);
        valorProperty?.SetValue(lancamento, Valor);
        tipoProperty?.SetValue(lancamento, (TipoLancamento)Tipo);
        dataProperty?.SetValue(lancamento, Data);
        descricaoProperty?.SetValue(lancamento, Descricao);

        return lancamento;
    }

    /// <summary>
    /// Cria uma entidade de persistência a partir do modelo de domínio
    /// </summary>
    public static LancamentoEntity FromDomain(Lancamento lancamento)
    {
        return new LancamentoEntity
        {
            Id = lancamento.Id,
            Valor = lancamento.Valor,
            Tipo = (short)lancamento.Tipo,
            Data = DateTime.SpecifyKind(lancamento.Data.Date, DateTimeKind.Utc),
            Descricao = lancamento.Descricao,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}