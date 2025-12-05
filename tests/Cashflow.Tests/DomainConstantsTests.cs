using Shouldly;

using Xunit;

namespace Cashflow.Tests;

public class DomainConstantsTests
{
    #region ValoresMonetarios

    [Fact]
    public void ValoresMonetarios_ValorMinimo_DeveSerZero()
    {
        DomainConstants.ValoresMonetarios.ValorMinimo.ShouldBe(0m);
    }

    [Fact]
    public void ValoresMonetarios_Precisao_DeveSerDezoito()
    {
        DomainConstants.ValoresMonetarios.Precisao.ShouldBe(18);
    }

    [Fact]
    public void ValoresMonetarios_Escala_DeveSerDois()
    {
        DomainConstants.ValoresMonetarios.Escala.ShouldBe(2);
    }

    #endregion

    #region LancamentoLimites

    [Fact]
    public void LancamentoLimites_DescricaoMaxLength_DeveSerQuinhentos()
    {
        DomainConstants.LancamentoLimites.DescricaoMaxLength.ShouldBe(500);
    }

    [Fact]
    public void LancamentoLimites_DiasPermitidosFuturos_DeveSerUm()
    {
        DomainConstants.LancamentoLimites.DiasPermitidosFuturos.ShouldBe(1);
    }

    #endregion

    #region Consolidacao

    [Fact]
    public void Consolidacao_PeriodoMaximoDias_DeveSerNoventaDias()
    {
        DomainConstants.Consolidacao.PeriodoMaximoDias.ShouldBe(90);
    }

    [Fact]
    public void Consolidacao_IncrementoDia_DeveSerUm()
    {
        DomainConstants.Consolidacao.IncrementoDia.ShouldBe(1);
    }

    #endregion

    #region Paginacao

    [Fact]
    public void Paginacao_PaginaMinima_DeveSerUm()
    {
        DomainConstants.Paginacao.PaginaMinima.ShouldBe(1);
    }

    [Fact]
    public void Paginacao_TamanhoPadrao_DeveSerDez()
    {
        DomainConstants.Paginacao.TamanhoPadrao.ShouldBe(10);
    }

    [Fact]
    public void Paginacao_TamanhoMaximo_DeveSerCem()
    {
        DomainConstants.Paginacao.TamanhoMaximo.ShouldBe(100);
    }

    [Fact]
    public void Paginacao_TamanhoMaximo_DeveSer_MaiorOuIgual_TamanhoPadrao()
    {
        DomainConstants.Paginacao.TamanhoMaximo.ShouldBeGreaterThanOrEqualTo(
            DomainConstants.Paginacao.TamanhoPadrao);
    }

    #endregion

    #region ValoresPadrao

    [Fact]
    public void ValoresPadrao_Zero_DeveSerZero()
    {
        DomainConstants.ValoresPadrao.Zero.ShouldBe(0m);
    }

    [Fact]
    public void ValoresPadrao_QuantidadeZero_DeveSerZero()
    {
        DomainConstants.ValoresPadrao.QuantidadeZero.ShouldBe(0);
    }

    #endregion
}