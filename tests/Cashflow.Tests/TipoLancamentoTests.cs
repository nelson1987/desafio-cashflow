using Shouldly;

using Xunit;

namespace Cashflow.Tests;

public class TipoLancamentoTests
{
    [Fact]
    public void TipoLancamento_Credito_DeveSerValor1()
    {
        ((int)TipoLancamento.Credito).ShouldBe(1);
    }

    [Fact]
    public void TipoLancamento_Debito_DeveSerValor2()
    {
        ((int)TipoLancamento.Debito).ShouldBe(2);
    }

    [Fact]
    public void TipoLancamento_ToString_DeveRetornarNomeCorreto()
    {
        TipoLancamento.Credito.ToString().ShouldBe("Credito");
        TipoLancamento.Debito.ToString().ShouldBe("Debito");
    }

    [Theory]
    [InlineData("Credito", TipoLancamento.Credito)]
    [InlineData("Debito", TipoLancamento.Debito)]
    public void TipoLancamento_Parse_DeveConverterCorretamente(string texto, TipoLancamento esperado)
    {
        Enum.Parse<TipoLancamento>(texto).ShouldBe(esperado);
    }

    [Theory]
    [InlineData(1, TipoLancamento.Credito)]
    [InlineData(2, TipoLancamento.Debito)]
    public void TipoLancamento_FromInt_DeveConverterCorretamente(int valor, TipoLancamento esperado)
    {
        ((TipoLancamento)valor).ShouldBe(esperado);
    }

    [Fact]
    public void TipoLancamento_DeveConterApenasDoisValores()
    {
        var valores = Enum.GetValues<TipoLancamento>();
        valores.Length.ShouldBe(2);
    }

    [Fact]
    public void TipoLancamento_IsDefined_DeveRetornarTrue_ParaValoresValidos()
    {
        Enum.IsDefined(typeof(TipoLancamento), 1).ShouldBeTrue();
        Enum.IsDefined(typeof(TipoLancamento), 2).ShouldBeTrue();
    }

    [Fact]
    public void TipoLancamento_IsDefined_DeveRetornarFalse_ParaValoresInvalidos()
    {
        Enum.IsDefined(typeof(TipoLancamento), 0).ShouldBeFalse();
        Enum.IsDefined(typeof(TipoLancamento), 3).ShouldBeFalse();
    }
}