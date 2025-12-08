namespace Cashflow;

/// <summary>
/// Representa um lançamento de débito ou crédito no fluxo de caixa
/// </summary>
public class Lancamento
{
    public Guid Id { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public DateTime Data { get; private set; }
    public string Descricao { get; private set; }

    private Lancamento()
    {
        Descricao = string.Empty;
    }

    public Lancamento(decimal valor, TipoLancamento tipo, DateTime data, string descricao)
    {
        ValidarValor(valor);
        ValidarDescricao(descricao);

        Id = Guid.NewGuid();
        Valor = valor;
        Tipo = tipo;
        // Normaliza a data para UTC para evitar problemas de fuso horário
        Data = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        Descricao = descricao;
    }

    /// <summary>
    /// Retorna o valor com sinal: positivo para crédito, negativo para débito
    /// </summary>
    public decimal ValorComSinal => Tipo == TipoLancamento.Credito ? Valor : -Valor;

    /// <summary>
    /// Verifica se o lançamento é do dia informado
    /// </summary>
    public bool EhDoDia(DateTime dia) => Data.Date == dia.Date;

    private static void ValidarValor(decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentException("O valor do lançamento deve ser maior que zero.", nameof(valor));
    }

    private static void ValidarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição do lançamento é obrigatória.", nameof(descricao));
    }
}