using Cashflow.Application.DTOs;

using FluentValidation;

namespace Cashflow.Application.Validators;

/// <summary>
/// Validador para criação de lançamento
/// </summary>
public class CriarLancamentoValidator : AbstractValidator<CriarLancamentoRequest>
{
    public CriarLancamentoValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(0)
            .WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Tipo)
            .IsInEnum()
            .WithMessage("Tipo de lançamento inválido. Use 1 para Crédito ou 2 para Débito.");

        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage("A data é obrigatória.")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("A data não pode ser futura.");

        RuleFor(x => x.Descricao)
            .NotEmpty()
            .WithMessage("A descrição é obrigatória.")
            .MaximumLength(500)
            .WithMessage("A descrição deve ter no máximo 500 caracteres.");
    }
}

