using Cashflow.Application.DTOs;

using FluentValidation;

using static Cashflow.Application.ApplicationConstants;
using static Cashflow.DomainConstants;

namespace Cashflow.Application.Validators;

/// <summary>
/// Validador para criação de lançamento
/// </summary>
public class CriarLancamentoValidator : AbstractValidator<CriarLancamentoRequest>
{
    public CriarLancamentoValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(ValoresMonetarios.ValorMinimo)
            .WithMessage(ValidacaoLancamento.ValorDeveSerMaiorQueZero);

        RuleFor(x => x.Tipo)
            .IsInEnum()
            .WithMessage(string.Format(
                ValidacaoLancamento.TipoInvalido,
                (int)TipoLancamento.Credito,
                (int)TipoLancamento.Debito));

        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage(ValidacaoLancamento.DataObrigatoria)
            .LessThanOrEqualTo(DateTime.Today.AddDays(LancamentoLimites.DiasPermitidosFuturos))
            .WithMessage(ValidacaoLancamento.DataNaoPodeFutura);

        RuleFor(x => x.Descricao)
            .NotEmpty()
            .WithMessage(ValidacaoLancamento.DescricaoObrigatoria)
            .MaximumLength(LancamentoLimites.DescricaoMaxLength)
            .WithMessage(string.Format(ValidacaoLancamento.DescricaoTamanhoMaximo, LancamentoLimites.DescricaoMaxLength));
    }
}