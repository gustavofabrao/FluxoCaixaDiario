using FluxoCaixaDiario.Domain.Enums;
using MediatR;
using System;
namespace FluxoCaixaDiario.SaldoDiario.Application.Commands
{
    public record ProcessTransactionEventCommand(Guid TransactionId, DateTime TransactionDate, decimal Amount, TransactionTypeEnum Type) : IRequest;
}