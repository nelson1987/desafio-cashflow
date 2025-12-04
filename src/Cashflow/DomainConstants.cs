namespace Cashflow;

/// <summary>
/// Constantes do domínio de fluxo de caixa
/// </summary>
public static class DomainConstants
{
    /// <summary>
    /// Constantes de valores monetários
    /// </summary>
    public static class ValoresMonetarios
    {
        /// <summary>
        /// Valor mínimo permitido para um lançamento
        /// </summary>
        public const decimal ValorMinimo = 0m;

        /// <summary>
        /// Precisão de casas decimais para valores monetários
        /// </summary>
        public const int Precisao = 18;

        /// <summary>
        /// Escala (casas decimais) para valores monetários
        /// </summary>
        public const int Escala = 2;
    }

    /// <summary>
    /// Constantes de validação de lançamento
    /// </summary>
    public static class LancamentoLimites
    {
        /// <summary>
        /// Tamanho máximo da descrição do lançamento
        /// </summary>
        public const int DescricaoMaxLength = 500;

        /// <summary>
        /// Dias futuros permitidos para data do lançamento
        /// </summary>
        public const int DiasPermitidosFuturos = 1;
    }

    /// <summary>
    /// Constantes de consolidação
    /// </summary>
    public static class Consolidacao
    {
        /// <summary>
        /// Período máximo em dias para consulta de relatório consolidado
        /// </summary>
        public const int PeriodoMaximoDias = 90;

        /// <summary>
        /// Incremento de dias para iteração de período
        /// </summary>
        public const int IncrementoDia = 1;
    }

    /// <summary>
    /// Constantes de paginação
    /// </summary>
    public static class Paginacao
    {
        /// <summary>
        /// Número mínimo de página
        /// </summary>
        public const int PaginaMinima = 1;

        /// <summary>
        /// Tamanho padrão de página
        /// </summary>
        public const int TamanhoPadrao = 10;

        /// <summary>
        /// Tamanho máximo de página permitido
        /// </summary>
        public const int TamanhoMaximo = 100;
    }

    /// <summary>
    /// Valores padrão para saldos zerados
    /// </summary>
    public static class ValoresPadrao
    {
        /// <summary>
        /// Valor zero para inicialização de saldos
        /// </summary>
        public const decimal Zero = 0m;

        /// <summary>
        /// Quantidade zero para inicialização de contadores
        /// </summary>
        public const int QuantidadeZero = 0;
    }
}