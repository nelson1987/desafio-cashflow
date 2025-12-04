namespace Cashflow.Application;

/// <summary>
/// Constantes da camada de aplicação
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Mensagens de erro para operações com lançamentos
    /// </summary>
    public static class ErrosLancamento
    {
        public const string ErroAoCriar = "Ocorreu um erro ao criar o lançamento.";
        public const string ErroAoBuscar = "Ocorreu um erro ao buscar o lançamento.";
        public const string ErroAoListar = "Ocorreu um erro ao listar os lançamentos.";
        public const string NaoEncontrado = "Lançamento com ID {0} não encontrado.";
    }

    /// <summary>
    /// Mensagens de erro para operações com consolidado
    /// </summary>
    public static class ErrosConsolidado
    {
        public const string ErroAoBuscar = "Ocorreu um erro ao buscar o saldo consolidado.";
        public const string ErroAoRecalcular = "Ocorreu um erro ao recalcular o saldo consolidado.";
        public const string ErroAoGerarRelatorio = "Ocorreu um erro ao gerar o relatório consolidado.";
        public const string DataInicialMaiorQueFinal = "A data inicial não pode ser maior que a data final.";
        public const string PeriodoMaximoExcedido = "O período máximo permitido é de {0} dias.";
    }

    /// <summary>
    /// Mensagens de validação de lançamento
    /// </summary>
    public static class ValidacaoLancamento
    {
        public const string ValorDeveSerMaiorQueZero = "O valor deve ser maior que zero.";
        public const string TipoInvalido = "Tipo de lançamento inválido. Use {0} para Crédito ou {1} para Débito.";
        public const string DataObrigatoria = "A data é obrigatória.";
        public const string DataNaoPodeFutura = "A data não pode ser futura.";
        public const string DescricaoObrigatoria = "A descrição é obrigatória.";
        public const string DescricaoTamanhoMaximo = "A descrição deve ter no máximo {0} caracteres.";
    }

    /// <summary>
    /// Templates de log da aplicação
    /// </summary>
    public static class LogTemplates
    {
        // Lançamento
        public const string ValidacaoFalhou = "Validação falhou ao criar lançamento: {Errors}";
        public const string LancamentoCriadoSucesso = "Lançamento criado com sucesso. Id: {Id}, Tipo: {Tipo}, Valor: {Valor}";
        public const string EventoLancamentoCriadoPublicado = "Evento LancamentoCriado publicado. LancamentoId: {Id}";
        public const string ErroValidacaoDominio = "Erro de validação de domínio ao criar lançamento";
        public const string ErroInesperadoCriarLancamento = "Erro inesperado ao criar lançamento";
        public const string LancamentoNaoEncontrado = "Lançamento não encontrado. Id: {Id}";
        public const string ErroObterLancamentoPorId = "Erro ao obter lançamento por ID: {Id}";
        public const string ErroListarLancamentos = "Erro ao listar lançamentos. Página: {Pagina}, Tamanho: {Tamanho}";
        public const string ErroObterLancamentosPorData = "Erro ao obter lançamentos por data: {Data}";

        // Consolidado
        public const string SaldoNaoEncontrado = "Saldo consolidado não encontrado para data: {Data}. Retornando saldo vazio.";
        public const string ErroObterSaldoPorData = "Erro ao obter saldo consolidado para data: {Data}";
        public const string ErroObterRelatorio = "Erro ao obter relatório consolidado. Período: {DataInicio} a {DataFim}";
        public const string IniciandoRecalculo = "Iniciando recálculo do saldo consolidado para data: {Data}";
        public const string SaldoRecalculado = "Saldo consolidado recalculado. Data: {Data}, Saldo: {Saldo}, Lançamentos: {Qtd}";
        public const string ErroRecalcular = "Erro ao recalcular saldo consolidado para data: {Data}";
    }
}