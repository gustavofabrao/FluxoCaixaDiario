using FluxoCaixaDiario.Domain.Entities;
using FluxoCaixaDiario.Domain.Events;
using FluxoCaixaDiario.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public class RegisterTransactionCommandHandler : IRequestHandler<RegisterTransactionCommand, Guid>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IRabbitMqPublisher _messagePublisher;
        private readonly IConfiguration _configuration;

        public RegisterTransactionCommandHandler(ITransactionRepository transactionRepository, 
            IRabbitMqPublisher messagePublisher,
            IConfiguration configuration)
        {
            _transactionRepository = transactionRepository;
            _messagePublisher = messagePublisher;
            _configuration = configuration;
        }

        public async Task<Guid> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = request.Valor,
                Type = request.Tipo,
                Date = request.Data,
                Description = request.Descricao
            };

            await _transactionRepository.AddAsync(transaction);

            var transactionRegisteredEvent = new TransactionRegisteredEvent
            {
                TransactionId = transaction.Id,
                TransactionDate = transaction.Date,
                Amount = transaction.Amount,
                Type = transaction.Type
            };

            await _messagePublisher.PublishAsync(_configuration["RabbitMQ:ExchangeName"], _configuration["RabbitMQ:RoutingKey"], transactionRegisteredEvent);

            return transaction.Id;
        }
    }
}