using FluxoCaixaDiario.Domain.Enums;
using MediatR;
using System;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public record RegisterTransactionCommand(TransactionTypeEnum Tipo, decimal Valor, string Descricao, DateTime Data) : IRequest<Guid>;
}