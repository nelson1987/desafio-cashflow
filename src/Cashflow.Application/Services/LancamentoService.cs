using Cashflow.Abstractions;
using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.Events;

using FluentValidation;

using Microsoft.Extensions.Logging;

using static Cashflow.Application.ApplicationConstants;
using static Cashflow.DomainConstants;

namespace Cashflow.Application.Services;

/// <summary>
/// Serviço de lançamentos (Use Case)
/// </summary>
public class LancamentoService : ILancamentoService
{
    private readonly ILancamentoRepository _repository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IValidator<CriarLancamentoRequest> _validator;
    private readonly ILogger<LancamentoService> _logger;

    public LancamentoService(
        ILancamentoRepository repository,
        IMessagePublisher messagePublisher,
        IValidator<CriarLancamentoRequest> validator,
        ILogger<LancamentoService> logger)
    {
        _repository = repository;
        _messagePublisher = messagePublisher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LancamentoResponse>> CriarAsync(
        CriarLancamentoRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validação
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            _logger.LogWarning(LogTemplates.ValidacaoFalhou, string.Join(", ", errors));
            return Result.Failure<LancamentoResponse>(errors);
        }

        try
        {
            // 2. Cria a entidade de domínio
            var lancamento = new Lancamento(
                valor: request.Valor,
                tipo: request.Tipo,
                data: request.Data,
                descricao: request.Descricao);

            // 3. Persiste no banco
            await _repository.AdicionarAsync(lancamento, cancellationToken);

            _logger.LogInformation(
                LogTemplates.LancamentoCriadoSucesso,
                lancamento.Id,
                lancamento.Tipo,
                lancamento.Valor);

            // 4. Publica evento para processamento assíncrono
            var evento = new LancamentoCriadoEvent(lancamento);
            await _messagePublisher.PublicarAsync(evento, cancellationToken);

            _logger.LogDebug(LogTemplates.EventoLancamentoCriadoPublicado, lancamento.Id);

            // 5. Retorna resposta
            return Result.Success(LancamentoResponse.FromDomain(lancamento));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, LogTemplates.ErroValidacaoDominio);
            return Result.Failure<LancamentoResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroInesperadoCriarLancamento);
            return Result.Failure<LancamentoResponse>(ErrosLancamento.ErroAoCriar);
        }
    }

    public async Task<Result<LancamentoResponse>> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lancamento = await _repository.ObterPorIdAsync(id, cancellationToken);

            if (lancamento is null)
            {
                _logger.LogDebug(LogTemplates.LancamentoNaoEncontrado, id);
                return Result.Failure<LancamentoResponse>(string.Format(ErrosLancamento.NaoEncontrado, id));
            }

            return Result.Success(LancamentoResponse.FromDomain(lancamento));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroObterLancamentoPorId, id);
            return Result.Failure<LancamentoResponse>(ErrosLancamento.ErroAoBuscar);
        }
    }

    public async Task<Result<LancamentosListResponse>> ListarAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        if (pagina < Paginacao.PaginaMinima) pagina = Paginacao.PaginaMinima;
        if (tamanhoPagina < Paginacao.PaginaMinima) tamanhoPagina = Paginacao.TamanhoPadrao;
        if (tamanhoPagina > Paginacao.TamanhoMaximo) tamanhoPagina = Paginacao.TamanhoMaximo;

        try
        {
            var lancamentos = await _repository.ObterTodosAsync(pagina, tamanhoPagina, cancellationToken);
            var total = await _repository.ContarAsync(cancellationToken);

            var response = new LancamentosListResponse
            {
                Items = lancamentos.Select(LancamentoResponse.FromDomain),
                TotalItems = total,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroListarLancamentos, pagina, tamanhoPagina);
            return Result.Failure<LancamentosListResponse>(ErrosLancamento.ErroAoListar);
        }
    }

    public async Task<Result<IEnumerable<LancamentoResponse>>> ObterPorDataAsync(
        DateTime data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lancamentos = await _repository.ObterPorDataAsync(data, cancellationToken);
            var response = lancamentos.Select(LancamentoResponse.FromDomain);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogTemplates.ErroObterLancamentosPorData, data);
            return Result.Failure<IEnumerable<LancamentoResponse>>(ErrosLancamento.ErroAoBuscar);
        }
    }
}