using System.Text;
using System.Text.Json;

using Cashflow.Abstractions;
using Cashflow.Events;
using Cashflow.Infrastructure.Messaging;
using Cashflow.IntegrationTests.Fixtures;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shouldly;

namespace Cashflow.IntegrationTests.Messaging;

/// <summary>
/// Testes de integração para RabbitMQ
/// </summary>
[Collection(RabbitMqCollection.Name)]
public class RabbitMqIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainerFixture _fixture;

    public RabbitMqIntegrationTests(RabbitMqContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.PurgeQueueAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.PurgeQueueAsync();
    }

    [Fact]
    public async Task PublishMessage_DevePersistirMensagemNaFila()
    {
        // Arrange
        var message = new TestMessage 
        { 
            Id = Guid.NewGuid(), 
            Content = "Teste de publicação",
            Timestamp = DateTime.UtcNow 
        };

        // Act
        await _fixture.PublishMessageAsync("test.message", message);

        // Assert
        var receivedMessage = await _fixture.ConsumeMessageAsync<TestMessage>();
        receivedMessage.ShouldNotBeNull();
        receivedMessage.Id.ShouldBe(message.Id);
        receivedMessage.Content.ShouldBe("Teste de publicação");
    }

    [Fact]
    public async Task RabbitMqPublisher_DevePublicarMensagemComSucesso()
    {
        // Arrange
        var settings = Options.Create(new RabbitMqSettings
        {
            Host = _fixture.Host,
            Port = _fixture.Port,
            Username = _fixture.Username,
            Password = _fixture.Password,
            VirtualHost = "/",
            Exchange = RabbitMqContainerFixture.TestExchange,
            ExchangeType = "topic",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = 5
        });

        await using var publisher = new RabbitMqPublisher(
            settings,
            NullLogger<RabbitMqPublisher>.Instance);

        var lancamento = new Lancamento(
            valor: 1000m,
            tipo: TipoLancamento.Credito,
            data: DateTime.Today,
            descricao: "Teste de evento");

        var evento = new LancamentoCriadoEvent(lancamento);

        // Act
        await publisher.PublicarAsync("lancamento.criado", evento);

        // Assert
        var receivedEvent = await _fixture.ConsumeMessageAsync<LancamentoCriadoEvent>();
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Valor.ShouldBe(1000m);
        receivedEvent.Tipo.ShouldBe(TipoLancamento.Credito);
        receivedEvent.LancamentoId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RabbitMq_DeveManterOrdemDasMensagens()
    {
        // Arrange
        var mensagens = new List<TestMessage>();
        for (int i = 0; i < 10; i++)
        {
            mensagens.Add(new TestMessage
            {
                Id = Guid.NewGuid(),
                Content = $"Mensagem {i}",
                Sequence = i,
                Timestamp = DateTime.UtcNow
            });
        }

        // Act
        foreach (var msg in mensagens)
        {
            await _fixture.PublishMessageAsync("test.sequence", msg);
        }

        // Assert - Consome as mensagens na ordem
        for (int i = 0; i < 10; i++)
        {
            var received = await _fixture.ConsumeMessageAsync<TestMessage>();
            received.ShouldNotBeNull();
            received.Sequence.ShouldBe(i);
            received.Content.ShouldBe($"Mensagem {i}");
        }
    }

    [Fact]
    public async Task RabbitMq_DevePublicarMensagemPersistente()
    {
        // Arrange
        var channel = await _fixture.CreateChannelAsync();

        var message = new TestMessage
        {
            Id = Guid.NewGuid(),
            Content = "Mensagem persistente"
        };

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        // Act
        await channel.BasicPublishAsync(
            exchange: RabbitMqContainerFixture.TestExchange,
            routingKey: "test.persistent",
            mandatory: false,
            basicProperties: properties,
            body: body);

        // Assert
        var received = await _fixture.ConsumeMessageAsync<TestMessage>();
        received.ShouldNotBeNull();
        received.Id.ShouldBe(message.Id);

        await channel.CloseAsync();
        channel.Dispose();
    }

    [Fact]
    public async Task RabbitMq_DeveSuportarMultiplasPublicacoesConcorrentes()
    {
        // Arrange
        var quantidadeMensagens = 50;
        var tarefas = new List<Task>();
        var mensagensPublicadas = new List<Guid>();

        // Act
        for (int i = 0; i < quantidadeMensagens; i++)
        {
            var id = Guid.NewGuid();
            mensagensPublicadas.Add(id);
            
            tarefas.Add(_fixture.PublishMessageAsync("test.concurrent", new TestMessage
            {
                Id = id,
                Content = $"Concorrente {i}",
                Sequence = i
            }));
        }

        await Task.WhenAll(tarefas);

        // Assert - Verifica se todas as mensagens foram recebidas
        var mensagensRecebidas = new List<Guid>();
        for (int i = 0; i < quantidadeMensagens; i++)
        {
            var msg = await _fixture.ConsumeMessageAsync<TestMessage>(10000);
            if (msg != null)
            {
                mensagensRecebidas.Add(msg.Id);
            }
        }

        mensagensRecebidas.Count.ShouldBe(quantidadeMensagens);
        mensagensRecebidas.ShouldBe(mensagensPublicadas, ignoreOrder: true);
    }

    [Fact]
    public async Task RabbitMq_ExchangeDeclare_DeveCriarExchangeCorretamente()
    {
        // Arrange
        var channel = await _fixture.CreateChannelAsync();
        var exchangeName = "test.exchange.new";

        // Act
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Assert - Se chegou aqui sem exceção, o exchange foi criado
        // Vamos tentar declarar passivamente para verificar
        await channel.ExchangeDeclarePassiveAsync(exchangeName);

        // Cleanup
        await channel.ExchangeDeleteAsync(exchangeName);
        await channel.CloseAsync();
        channel.Dispose();
    }

    [Fact]
    public async Task RabbitMq_QueueDeclare_DeveCriarFilaCorretamente()
    {
        // Arrange
        var channel = await _fixture.CreateChannelAsync();
        var queueName = "test.queue.new";

        // Act
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Assert
        var queueInfo = await channel.QueueDeclarePassiveAsync(queueName);
        queueInfo.QueueName.ShouldBe(queueName);

        // Cleanup
        await channel.QueueDeleteAsync(queueName);
        await channel.CloseAsync();
        channel.Dispose();
    }

    [Fact]
    public async Task LancamentoCriadoEvent_DeveSerPublicadoCorretamente()
    {
        // Arrange
        var lancamento = new Lancamento(
            valor: 2500.50m,
            tipo: TipoLancamento.Debito,
            data: new DateTime(2024, 6, 15),
            descricao: "Pagamento fornecedor");

        var evento = new LancamentoCriadoEvent(lancamento);

        // Act
        await _fixture.PublishMessageAsync(RoutingKeys.LancamentoCriado, evento);

        // Assert
        var received = await _fixture.ConsumeMessageAsync<LancamentoCriadoEvent>();
        received.ShouldNotBeNull();
        received.LancamentoId.ShouldBe(evento.LancamentoId);
        received.Valor.ShouldBe(2500.50m);
        received.Tipo.ShouldBe(TipoLancamento.Debito);
        received.Data.ShouldBe(new DateTime(2024, 6, 15));
    }

    [Fact]
    public async Task RabbitMq_BasicConsume_DeveConsumirMensagemComAck()
    {
        // Arrange
        var channel = await _fixture.CreateChannelAsync();
        var message = new TestMessage
        {
            Id = Guid.NewGuid(),
            Content = "Mensagem com ACK"
        };

        await _fixture.PublishMessageAsync("test.ack", message);

        // Act
        var tcs = new TaskCompletionSource<TestMessage?>();
        var consumer = new AsyncEventingBasicConsumer(channel);
        
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var receivedMessage = JsonSerializer.Deserialize<TestMessage>(json);
            
            await channel.BasicAckAsync(ea.DeliveryTag, false);
            tcs.SetResult(receivedMessage);
        };

        await channel.BasicConsumeAsync(
            queue: RabbitMqContainerFixture.TestQueue,
            autoAck: false,
            consumer: consumer);

        var result = await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        if (result == tcs.Task)
        {
            var received = await tcs.Task;
            received.ShouldNotBeNull();
            received.Id.ShouldBe(message.Id);
        }
        else
        {
            throw new TimeoutException("Timeout ao aguardar mensagem");
        }

        await channel.CloseAsync();
        channel.Dispose();
    }

    private class TestMessage
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
