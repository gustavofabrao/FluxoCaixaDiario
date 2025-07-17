using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using FluxoCaixaDiario.Lancamentos.Services.IServices;
using FluxoCaixaDiario.Shared.Entities;
using FluxoCaixaDiario.Shared.Enums;
using FluxoCaixaDiario.Shared.Events;
using FluxoCaixaDiario.Shared.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands
{
    public class RegisterTransactionCommandHandler : IRequestHandler<RegisterTransactionCommand, Guid>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IRedisMessageBuffer _messageBuffer;
        private readonly IConfiguration _configuration;
        private readonly IDatabase _redisDb;
        private readonly ILogger<RegisterTransactionCommandHandler> _logger;

        public RegisterTransactionCommandHandler(ITransactionRepository transactionRepository,
            IRedisMessageBuffer messageBuffer,
            ILogger<RegisterTransactionCommandHandler> logger,
            IConnectionMultiplexer redisConnection,
            IConfiguration configuration)
        {
            _transactionRepository = transactionRepository;
            _messageBuffer = messageBuffer;
            _logger = logger;
            _configuration = configuration;
            _redisDb = redisConnection.GetDatabase();
        }

        public async Task<Guid> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
        {
            // Configuração da política de retry para operações no Redis
            var redisRetryPolicy = Policy
           .Handle<RedisConnectionException>() 
           .Or<RedisTimeoutException>()       
           .WaitAndRetryAsync(3,             
               retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
               (exception, timeSpan, retryCount, context) =>
               {
                   _logger.LogWarning(exception, "Falha ao persistir transação no Redis. Tentando novamente em {TimeSpan} segundos ({RetryCount}/{MaxRetries})", timeSpan.TotalSeconds, retryCount, 3);
               });

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = request.Valor,
                Type = (TransactionTypeEnum)request.Tipo.Value,
                Date = request.Data.Value,
                Description = request.Descricao
            };
            try
            {
                // Executa a operação de escrita no Redis com a política de retry
                await redisRetryPolicy.ExecuteAsync(async () =>
                {      
                    await _transactionRepository.AddAsync(transaction);

                    var transactionRegisteredEvent = new TransactionRegisteredEvent
                    {
                        TransactionId = transaction.Id,
                        TransactionDate = transaction.Date,
                        Amount = transaction.Amount,
                        Type = transaction.Type
                    };

                    await _messageBuffer.AddMessageAsync(_configuration["Redis:BatchKey"], transactionRegisteredEvent);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha FINAL ao persistir transação {TransactionId} no Redis após todas as retentativas.", transaction.Id);
                throw;
            } 

            return transaction.Id;
        }
    }
}