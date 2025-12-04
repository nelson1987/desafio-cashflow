using Cashflow.Abstractions;
using Cashflow.Application.Abstractions;
using Cashflow.Application.DTOs;
using Cashflow.Events;

using FluentValidation;

using Microsoft.Extensions.Logging;

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
            _logger.LogWarning("Validação falhou ao criar lançamento: {Errors}", string.Join(", ", errors));
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
                "Lançamento criado com sucesso. Id: {Id}, Tipo: {Tipo}, Valor: {Valor}",
                lancamento.Id,
                lancamento.Tipo,
                lancamento.Valor);

            // 4. Publica evento para processamento assíncrono
            var evento = new LancamentoCriadoEvent(lancamento);
            await _messagePublisher.PublicarAsync(evento, cancellationToken);

            _logger.LogDebug("Evento LancamentoCriado publicado. LancamentoId: {Id}", lancamento.Id);

            // 5. Retorna resposta
            return Result.Success(LancamentoResponse.FromDomain(lancamento));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Erro de validação de domínio ao criar lançamento");
            return Result.Failure<LancamentoResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar lançamento");
            return Result.Failure<LancamentoResponse>("Ocorreu um erro ao criar o lançamento.");
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
                _logger.LogDebug("Lançamento não encontrado. Id: {Id}", id);
                return Result.Failure<LancamentoResponse>($"Lançamento com ID {id} não encontrado.");
            }

            return Result.Success(LancamentoResponse.FromDomain(lancamento));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter lançamento por ID: {Id}", id);
            return Result.Failure<LancamentoResponse>("Ocorreu um erro ao buscar o lançamento.");
        }
    }

    public async Task<Result<LancamentosListResponse>> ListarAsync(
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1) pagina = 1;
        if (tamanhoPagina < 1) tamanhoPagina = 10;
        if (tamanhoPagina > 100) tamanhoPagina = 100;

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
            _logger.LogError(ex, "Erro ao listar lançamentos. Página: {Pagina}, Tamanho: {Tamanho}", pagina, tamanhoPagina);
            return Result.Failure<LancamentosListResponse>("Ocorreu um erro ao listar os lançamentos.");
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
            _logger.LogError(ex, "Erro ao obter lançamentos por data: {Data}", data);
            return Result.Failure<IEnumerable<LancamentoResponse>>("Ocorreu um erro ao buscar os lançamentos.");
        }
    }
}

