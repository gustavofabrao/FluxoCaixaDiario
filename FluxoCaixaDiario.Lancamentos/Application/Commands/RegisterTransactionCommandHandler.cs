using FluxoCaixaDiario.Domain.Entities;
using FluxoCaixaDiario.Domain.Events;
using FluxoCaixaDiario.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public class RegisterTransactionCommandHandler : IRequestHandler<RegisterTransactionCommand, Guid>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IRabbitMqPublisher _messagePublisher;

        public RegisterTransactionCommandHandler(ITransactionRepository transactionRepository, IRabbitMqPublisher messagePublisher)
        {
            _transactionRepository = transactionRepository;
            _messagePublisher = messagePublisher;
        }

        public async Task<Guid> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                Type = request.Type,
                Date = DateTime.UtcNow,
                Description = request.Description
            };

            await _transactionRepository.AddAsync(transaction);

            var transactionRegisteredEvent = new TransactionRegisteredEvent
            {
                TransactionId = transaction.Id,
                TransactionDate = transaction.Date,
                Amount = transaction.Amount,
                Type = transaction.Type
            };

            await _messagePublisher.PublishAsync("transaction_events", "transaction_registered", transactionRegisteredEvent);

            return transaction.Id;
        }
    }
}