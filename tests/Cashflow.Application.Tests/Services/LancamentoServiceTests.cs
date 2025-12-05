using Cashflow.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.Application.Services;
using Cashflow.Events;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

namespace Cashflow.Application.Tests.Services;

public class LancamentoServiceTests
{
    private readonly Mock<ILancamentoRepository> _repositoryMock;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly Mock<IValidator<CriarLancamentoRequest>> _validatorMock;
    private readonly Mock<ILogger<LancamentoService>> _loggerMock;
    private readonly LancamentoService _sut;

    public LancamentoServiceTests()
    {
        _repositoryMock = new Mock<ILancamentoRepository>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _validatorMock = new Mock<IValidator<CriarLancamentoRequest>>();
        _loggerMock = new Mock<ILogger<LancamentoService>>();

        _sut = new LancamentoService(
            _repositoryMock.Object,
            _messagePublisherMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    #region CriarAsync

    [Fact]
    public async Task CriarAsync_DeveRetornarSucesso_QuandoLancamentoValido()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100.50m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Venda de produto"
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lancamento l, CancellationToken _) => l);

        // Act
        var result = await _sut.CriarAsync(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Valor.ShouldBe(request.Valor);
        result.Value.Tipo.ShouldBe(request.Tipo.ToString());
        result.Value.Descricao.ShouldBe(request.Descricao);
    }

    [Fact]
    public async Task CriarAsync_DevePublicarEvento_QuandoLancamentoCriado()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        await _sut.CriarAsync(request);

        // Assert
        _messagePublisherMock.Verify(
            p => p.PublicarAsync(It.IsAny<LancamentoCriadoEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeveRetornarFalha_QuandoValidacaoFalha()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 0, // Inválido
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = ""
        };

        var validationFailure = new ValidationResult(new[]
        {
            new ValidationFailure("Valor", "O valor deve ser maior que zero.")
        });

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationFailure);

        // Act
        var result = await _sut.CriarAsync(request);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Contains("maior que zero"));
    }

    [Fact]
    public async Task CriarAsync_NaoDevePersistir_QuandoValidacaoFalha()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 0,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = ""
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Valor", "Erro") }));

        // Act
        await _sut.CriarAsync(request);

        // Assert
        _repositoryMock.Verify(
            r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CriarAsync_NaoDevePublicarEvento_QuandoValidacaoFalha()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 0,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = ""
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Valor", "Erro") }));

        // Act
        await _sut.CriarAsync(request);

        // Assert
        _messagePublisherMock.Verify(
            p => p.PublicarAsync(It.IsAny<LancamentoCriadoEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Today,
            Descricao = "Teste"
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro de banco"));

        // Act
        var result = await _sut.CriarAsync(request);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosLancamento.ErroAoCriar);
    }

    #endregion

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarLancamento_QuandoExiste()
    {
        // Arrange
        var lancamento = new Lancamento(100m, TipoLancamento.Credito, DateTime.Today, "Venda");

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(lancamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamento);

        // Act
        var result = await _sut.ObterPorIdAsync(lancamento.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(lancamento.Id);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarFalha_QuandoNaoExiste()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lancamento?)null);

        // Act
        var result = await _sut.ObterPorIdAsync(id);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(id.ToString());
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro"));

        // Act
        var result = await _sut.ObterPorIdAsync(id);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosLancamento.ErroAoBuscar);
    }

    #endregion

    #region ListarAsync

    [Fact]
    public async Task ListarAsync_DeveRetornarListaPaginada()
    {
        // Arrange
        var lancamentos = new List<Lancamento>
        {
            new(100m, TipoLancamento.Credito, DateTime.Today, "Venda 1"),
            new(50m, TipoLancamento.Debito, DateTime.Today, "Compra 1")
        };

        _repositoryMock
            .Setup(r => r.ObterTodosAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamentos);

        _repositoryMock
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _sut.ListarAsync(1, 10);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Items.Count().ShouldBe(2);
        result.Value.TotalItems.ShouldBe(2);
        result.Value.Pagina.ShouldBe(1);
        result.Value.TamanhoPagina.ShouldBe(10);
    }

    [Theory]
    [InlineData(0, 10, 1, 10)]   // Página 0 deve virar 1
    [InlineData(-1, 10, 1, 10)] // Página negativa deve virar 1
    [InlineData(1, 0, 1, 10)]   // Tamanho 0 deve virar 10 (padrão)
    [InlineData(1, -1, 1, 10)]  // Tamanho negativo deve virar 10 (padrão)
    [InlineData(1, 200, 1, 100)] // Tamanho > 100 deve limitar a 100
    public async Task ListarAsync_DeveNormalizarParametrosPaginacao(
        int paginaInput, int tamanhoInput, int paginaExpected, int tamanhoExpected)
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterTodosAsync(paginaExpected, tamanhoExpected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lancamento>());

        _repositoryMock
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _sut.ListarAsync(paginaInput, tamanhoInput);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Pagina.ShouldBe(paginaExpected);
        result.Value.TamanhoPagina.ShouldBe(tamanhoExpected);
    }

    [Fact]
    public async Task ListarAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterTodosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro"));

        // Act
        var result = await _sut.ListarAsync(1, 10);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosLancamento.ErroAoListar);
    }

    #endregion

    #region ObterPorDataAsync

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarLancamentosDoDia()
    {
        // Arrange
        var data = new DateTime(2024, 1, 15);
        var lancamentos = new List<Lancamento>
        {
            new(100m, TipoLancamento.Credito, data, "Venda"),
            new(50m, TipoLancamento.Debito, data, "Compra")
        };

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamentos);

        // Act
        var result = await _sut.ObterPorDataAsync(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count().ShouldBe(2);
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarListaVazia_QuandoNaoHaLancamentos()
    {
        // Arrange
        var data = DateTime.Today;

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lancamento>());

        // Act
        var result = await _sut.ObterPorDataAsync(data);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarFalha_QuandoRepositorioLancaExcecao()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro"));

        // Act
        var result = await _sut.ObterPorDataAsync(DateTime.Today);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ApplicationConstants.ErrosLancamento.ErroAoBuscar);
    }

    #endregion
}

