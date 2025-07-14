using FluxoCaixaDiario.Domain.Enums;
using MediatR;
using System;
namespace FluxoCaixaDiario.SaldoDiario.Application.Commands
{
    public record ProcessTransactionEventCommand : IRequest
    {
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public TransactionTypeEnum Type { get; set; }
    }
}