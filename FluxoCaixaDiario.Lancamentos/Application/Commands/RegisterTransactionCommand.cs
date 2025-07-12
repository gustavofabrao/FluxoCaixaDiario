using FluxoCaixaDiario.Domain.Enums;
using MediatR;
using System;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public record RegisterTransactionCommand(decimal Amount, TransactionTypeEnum Type, string Description) : IRequest<Guid>;
}