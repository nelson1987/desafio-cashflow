namespace Cashflow.Abstractions;

/// <summary>
/// Interface para publicação de mensagens/eventos
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publica uma mensagem/evento de forma assíncrona
    /// </summary>
    Task PublicarAsync<T>(T mensagem, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publica uma mensagem/evento para um tópico específico
    /// </summary>
    Task PublicarAsync<T>(string topico, T mensagem, CancellationToken cancellationToken = default) where T : class;
}
