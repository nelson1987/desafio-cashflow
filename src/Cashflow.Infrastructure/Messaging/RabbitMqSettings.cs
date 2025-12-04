namespace Cashflow.Infrastructure.Messaging;

/// <summary>
/// Configurações do RabbitMQ
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// Host do RabbitMQ (ex: localhost, rabbitmq)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Porta do RabbitMQ
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Usuário
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Senha
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual Host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Nome da exchange principal
    /// </summary>
    public string Exchange { get; set; } = "cashflow.events";

    /// <summary>
    /// Tipo da exchange (direct, topic, fanout, headers)
    /// </summary>
    public string ExchangeType { get; set; } = "topic";

    /// <summary>
    /// Se deve tentar reconectar automaticamente
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Intervalo de tentativa de reconexão em segundos
    /// </summary>
    public int NetworkRecoveryInterval { get; set; } = 10;
}

/// <summary>
/// Routing keys para os eventos
/// </summary>
public static class RoutingKeys
{
    public const string LancamentoCriado = "lancamento.criado";
    public const string SaldoRecalculado = "saldo.recalculado";
}

/// <summary>
/// Nomes das filas
/// </summary>
public static class QueueNames
{
    public const string ConsolidacaoLancamentos = "cashflow.consolidacao.lancamentos";
}


