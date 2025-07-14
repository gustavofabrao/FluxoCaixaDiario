using FluentValidation;
using FluxoCaixaDiario.Domain.Enums;
using System;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public class RegisterTransactionCommandValidator : AbstractValidator<RegisterTransactionCommand>
    {
        public RegisterTransactionCommandValidator()
        {
            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("A data é obrigatória")
                .LessThanOrEqualTo(DateTime.Today).WithMessage("A data do lançamento não pode ser futura");

            RuleFor(x => x.Descricao)
                .MaximumLength(200).WithMessage("A descrição não pode exceder 500 caracteres");

            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("O valor deve ser maior que zero");

            RuleFor(x => x.Tipo)
                .NotNull().WithMessage("O tipo de lançamento é obrigatório")
                .Must(x => x.Equals(TransactionTypeEnum.Credit) || x.Equals(TransactionTypeEnum.Debit))
                .WithMessage("O tipo de lançamento deve ser Crédito(0) ou Débito(1)");
        }
    }
}
