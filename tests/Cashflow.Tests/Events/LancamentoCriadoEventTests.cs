using Cashflow.Events;
using Shouldly;
using Xunit;

namespace Cashflow.Tests.Events;

public class LancamentoCriadoEventTests
{
    [Fact]
    public void Criar_ComLancamento_DevePopularPropriedadesCorretamente()
    {
        // Arrange
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, "Venda");

        // Act
        var evento = new LancamentoCriadoEvent(lancamento);

        // Assert
        evento.LancamentoId.ShouldBe(lancamento.Id);
        evento.Data.ShouldBe(lancamento.Data);
        evento.Tipo.ShouldBe(TipoLancamento.Credito);
        evento.Valor.ShouldBe(100m);
    }

    [Fact]
    public void Criar_SemParametros_DevePermitirInicializacao()
    {
        // Act
        var evento = new LancamentoCriadoEvent();

        // Assert
        evento.LancamentoId.ShouldBe(Guid.Empty);
        evento.Valor.ShouldBe(0m);
    }

    [Fact]
    public void Criar_ComLancamentoDebito_DeveDefinirTipoCorreto()
    {
        // Arrange
        var lancamento = new Lancamento(50m, TipoLancamento.Debito, DateTime.Today, "Pagamento");

        // Act
        var evento = new LancamentoCriadoEvent(lancamento);

        // Assert
        evento.Tipo.ShouldBe(TipoLancamento.Debito);
    }

    [Fact]
    public void CriadoEm_DeveSerDefinidoAutomaticamente()
    {
        // Arrange
        var antesDoEvento = DateTime.UtcNow;
        
        // Act
        var evento = new LancamentoCriadoEvent();
        var depoisDoEvento = DateTime.UtcNow;

        // Assert
        evento.CriadoEm.ShouldBeGreaterThanOrEqualTo(antesDoEvento);
        evento.CriadoEm.ShouldBeLessThanOrEqualTo(depoisDoEvento);
    }

    [Fact]
    public void Criar_ComInitSyntax_DevePermitirInicializacao()
    {
        // Act
        var evento = new LancamentoCriadoEvent
        {
            LancamentoId = Guid.NewGuid(),
            Data = DateTime.Today,
            Tipo = TipoLancamento.Credito,
            Valor = 250m
        };

        // Assert
        evento.LancamentoId.ShouldNotBe(Guid.Empty);
        evento.Valor.ShouldBe(250m);
        evento.Tipo.ShouldBe(TipoLancamento.Credito);
    }
}

