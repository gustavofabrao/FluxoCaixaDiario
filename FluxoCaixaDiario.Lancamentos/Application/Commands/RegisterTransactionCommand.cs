using FluxoCaixaDiario.Domain.Enums;
using MediatR;
using System;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public record RegisterTransactionCommand : IRequest<Guid>
    {
        public DateTime Data { get; set; }
        public string Descricao { get; set; }
        public decimal Valor { get; set; }
        public TransactionTypeEnum Tipo { get; set; }
    }
}