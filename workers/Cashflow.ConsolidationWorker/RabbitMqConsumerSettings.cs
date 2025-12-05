namespace Cashflow.ConsolidationWorker;

/// <summary>
/// Configurações do consumidor RabbitMQ para o Worker.
/// </summary>
public class RabbitMqConsumerSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "cashflow.events";
    public string Queue { get; set; } = "cashflow.consolidation";
    public string RoutingKey { get; set; } = "lancamento.#";
}