using Cashflow.Infrastructure.Configuration;

using static Cashflow.Infrastructure.InfrastructureConstants;

namespace Cashflow.Infrastructure.Messaging;

/// <summary>
/// Configurações do RabbitMQ
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = ConfigurationSections.RabbitMQ;

    /// <summary>
    /// Host do RabbitMQ (ex: localhost, rabbitmq)
    /// </summary>
    public string Host { get; set; } = RabbitMqDefaults.Host;

    /// <summary>
    /// Porta do RabbitMQ
    /// </summary>
    public int Port { get; set; } = InfrastructureSettings.RabbitMq.PortaPadrao;

    /// <summary>
    /// Usuário
    /// </summary>
    public string Username { get; set; } = RabbitMqDefaults.Username;

    /// <summary>
    /// Senha
    /// </summary>
    public string Password { get; set; } = RabbitMqDefaults.Password;

    /// <summary>
    /// Virtual Host
    /// </summary>
    public string VirtualHost { get; set; } = RabbitMqDefaults.VirtualHost;

    /// <summary>
    /// Nome da exchange principal
    /// </summary>
    public string Exchange { get; set; } = RabbitMqDefaults.Exchange;

    /// <summary>
    /// Tipo da exchange (direct, topic, fanout, headers)
    /// </summary>
    public string ExchangeType { get; set; } = RabbitMqDefaults.ExchangeType;

    /// <summary>
    /// Se deve tentar reconectar automaticamente
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Intervalo de tentativa de reconexão em segundos
    /// </summary>
    public int NetworkRecoveryInterval { get; set; } = InfrastructureSettings.RabbitMq.NetworkRecoveryIntervalPadrao;
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