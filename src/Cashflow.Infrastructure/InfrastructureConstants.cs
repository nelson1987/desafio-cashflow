namespace Cashflow.Infrastructure;

/// <summary>
/// Constantes da camada de infraestrutura
/// </summary>
public static class InfrastructureConstants
{
    /// <summary>
    /// Nomes de connection strings
    /// </summary>
    public static class ConnectionStrings
    {
        public const string PostgreSQL = "PostgreSQL";
        public const string Redis = "Redis";
    }

    /// <summary>
    /// Nomes de variáveis de ambiente
    /// </summary>
    public static class EnvironmentVariables
    {
        public const string PostgreSqlConnectionString = "CONNECTION_STRING_POSTGRESQL";
        public const string RedisConnectionString = "CONNECTION_STRING_REDIS";
        public const string RabbitMqHost = "RABBITMQ_HOST";
        public const string RabbitMqPort = "RABBITMQ_PORT";
        public const string RabbitMqUser = "RABBITMQ_USER";
        public const string RabbitMqPassword = "RABBITMQ_PASSWORD";
        public const string RabbitMqVHost = "RABBITMQ_VHOST";
        public const string RabbitMqExchange = "RABBITMQ_EXCHANGE";
        public const string RabbitMqExchangeType = "RABBITMQ_EXCHANGE_TYPE";
        public const string RabbitMqAutomaticRecovery = "RABBITMQ_AUTOMATIC_RECOVERY";
        public const string RabbitMqNetworkRecoveryInterval = "RABBITMQ_NETWORK_RECOVERY_INTERVAL";
    }

    /// <summary>
    /// Configurações padrão do RabbitMQ
    /// </summary>
    public static class RabbitMqDefaults
    {
        public const string Host = "localhost";
        public const string Username = "guest";
        public const string Password = "guest";
        public const string VirtualHost = "/";
        public const string Exchange = "cashflow.events";
        public const string ExchangeType = "topic";
        public const string DeadLetterExchangeSuffix = ".dlx";
        public const string DeadLetterRoutingKeySuffix = ".dead";
    }

    /// <summary>
    /// Argumentos de fila RabbitMQ
    /// </summary>
    public static class RabbitMqQueueArguments
    {
        public const string DeadLetterExchange = "x-dead-letter-exchange";
        public const string DeadLetterRoutingKey = "x-dead-letter-routing-key";
    }

    /// <summary>
    /// Nomes de seções de configuração
    /// </summary>
    public static class ConfigurationSections
    {
        public const string RabbitMQ = "RabbitMQ";
        public const string Host = "Host";
        public const string Port = "Port";
        public const string Username = "Username";
        public const string Password = "Password";
        public const string VirtualHost = "VirtualHost";
        public const string Exchange = "Exchange";
        public const string ExchangeType = "ExchangeType";
        public const string AutomaticRecoveryEnabled = "AutomaticRecoveryEnabled";
        public const string NetworkRecoveryInterval = "NetworkRecoveryInterval";
    }

    /// <summary>
    /// Configurações de health checks
    /// </summary>
    public static class HealthChecks
    {
        public const string PostgreSqlName = "postgresql";
        public const string RedisName = "redis";
        public const string RabbitMqName = "rabbitmq";

        public static readonly string[] PostgreSqlTags = ["db", "sql", "postgresql"];
        public static readonly string[] RedisTags = ["cache", "redis"];
        public static readonly string[] RabbitMqTags = ["messaging", "rabbitmq"];
    }

    /// <summary>
    /// Nomes de tabelas do banco de dados
    /// </summary>
    public static class Tables
    {
        public const string Lancamentos = "lancamentos";
        public const string SaldosConsolidados = "saldos_consolidados";
    }

    /// <summary>
    /// Nomes de colunas do banco de dados
    /// </summary>
    public static class Columns
    {
        // Comuns
        public const string Id = "id";
        public const string Data = "data";
        public const string CreatedAt = "created_at";
        public const string UpdatedAt = "updated_at";

        // Lançamentos
        public const string Valor = "valor";
        public const string Tipo = "tipo";
        public const string Descricao = "descricao";

        // Saldos Consolidados
        public const string TotalCreditos = "total_creditos";
        public const string TotalDebitos = "total_debitos";
        public const string Saldo = "saldo";
        public const string QuantidadeLancamentos = "quantidade_lancamentos";
        public const string ProcessadoEm = "processado_em";
    }

    /// <summary>
    /// Nomes de índices do banco de dados
    /// </summary>
    public static class Indexes
    {
        public const string LancamentosData = "idx_lancamentos_data";
        public const string LancamentosTipo = "idx_lancamentos_tipo";
        public const string LancamentosDataTipo = "idx_lancamentos_data_tipo";
    }

    /// <summary>
    /// Expressões SQL padrão
    /// </summary>
    public static class SqlDefaults
    {
        public const string UuidGenerateV4 = "gen_random_uuid()";
        public const string CurrentTimestamp = "CURRENT_TIMESTAMP";
        public const string DateColumnType = "date";
    }

    /// <summary>
    /// Content types para mensageria
    /// </summary>
    public static class ContentTypes
    {
        public const string ApplicationJson = "application/json";
    }

    /// <summary>
    /// Templates de log da infraestrutura
    /// </summary>
    public static class LogTemplates
    {
        // RabbitMQ Publisher
        public const string RetryPublicacao = "Tentativa {Attempt} de publicação falhou. Tentando novamente em {Delay}ms";
        public const string CircuitBreakerAbertoRabbitMq = "Circuit breaker ABERTO para RabbitMQ";
        public const string CircuitBreakerFechadoRabbitMq = "Circuit breaker FECHADO para RabbitMQ";
        public const string MensagemPublicada = "Mensagem publicada. Exchange: {Exchange}, RoutingKey: {RoutingKey}, Type: {Type}";
        public const string CircuitBreakerMensagemNaoPublicada = "Circuit breaker aberto. Mensagem não publicada. Tipo: {Type}";
        public const string ErroPublicarMensagem = "Erro ao publicar mensagem. Tipo: {Type}, Tópico: {Topico}";
        public const string ConexaoRabbitMqEstabelecida = "Conexão com RabbitMQ estabelecida. Host: {Host}:{Port}";

        // RabbitMQ Consumer
        public const string MensagemRecebida = "Mensagem recebida. Queue: {Queue}, MessageId: {MessageId}";
        public const string MensagemDeserializadaNull = "Mensagem deserializada como null. Queue: {Queue}";
        public const string ErroProcessarMensagem = "Erro ao processar mensagem. Queue: {Queue}, Body: {Body}";
        public const string ConsumidorIniciado = "Consumidor iniciado. Queue: {Queue}";
        public const string FilaConfigurada = "Fila configurada. Queue: {Queue}, RoutingKey: {RoutingKey}";
        public const string ParandoConsumidor = "Parando consumidor. Queue: {Queue}";

        // Cache
        public const string RetryCache = "Tentativa {Attempt} de acesso ao cache falhou. Tentando novamente em {Delay}ms";
        public const string CircuitBreakerAbertoCache = "Circuit breaker ABERTO para o cache. Duração: {Duration}s";
        public const string CircuitBreakerFechadoCache = "Circuit breaker FECHADO para o cache";
        public const string ErroObterCache = "Erro ao obter valor do cache. Chave: {Chave}";
        public const string ErroDefinirCache = "Erro ao definir valor no cache. Chave: {Chave}";
        public const string ErroRemoverCache = "Erro ao remover valor do cache. Chave: {Chave}";
        public const string RemoverPorPrefixoNaoSuportado = "RemoverPorPrefixoAsync não é suportado nativamente por IDistributedCache. Prefixo: {Prefixo}";
        public const string ErroVerificarExistenciaCache = "Erro ao verificar existência no cache. Chave: {Chave}";
        public const string CacheAtualizado = "Cache atualizado para saldo do dia {Data}";
        public const string SaldoRecalculadoCacheAtualizado = "Saldo recalculado e cache atualizado para {Data}";
    }

    /// <summary>
    /// Mensagens de erro
    /// </summary>
    public static class ErrorMessages
    {
        public const string ConnectionStringNaoEncontrada = "Connection string '{0}' não encontrada.";
    }
}