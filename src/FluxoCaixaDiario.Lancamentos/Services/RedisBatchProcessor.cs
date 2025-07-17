using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker; 
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Services
{
    public class RedisBatchProcessor : BackgroundService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisBatchProcessor> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConnectionFactory _rabbitMqFactory;
        private readonly IRabbitMqChannelAdapter _channel;

        private readonly string _redisBatchKey;
        private readonly int _batchSize;
        private readonly TimeSpan _batchInterval;
        private readonly string _rabbitMqQueueName;

        public RedisBatchProcessor(IConnectionMultiplexer redis, 
            ILogger<RedisBatchProcessor> logger, 
            IConfiguration configuration,
            IConnectionFactory connectionFactory,
            IRabbitMqChannelAdapter channel = null)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
            _configuration = configuration;
            _rabbitMqFactory = connectionFactory;

            _redisBatchKey = _configuration["Redis:BatchKey"] ?? "fila_consolidacao_diaria";
            _batchSize = int.Parse(_configuration["Redis:BatchSize"] ?? "100");
            _batchInterval = TimeSpan.FromSeconds(int.Parse(_configuration["Redis:BatchIntervalSeconds"] ?? "5"));

            if(connectionFactory == null)
            {
                _rabbitMqFactory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:Username"],
                    Password = _configuration["RabbitMQ:Password"]
                };
            }
            else
            {
                _rabbitMqFactory = connectionFactory;
            }
            _rabbitMqQueueName = _configuration["RabbitMQ:QueueName"] ?? "fila_consolidacao_diaria";
        }

        protected override async Task ExecuteAsync(CancellationToken cancelationToken)
        {
            _logger.LogInformation("RedisBatchProcessor  - Iniciado dentro da API de Lancamentos");

            while (!cancelationToken.IsCancellationRequested)
            {
                await Task.Delay(_batchInterval, cancelationToken);

                await ProcessBatchAsync(cancelationToken);
            }

            _logger.LogInformation("RedisBatchProcessor - Finalizado dentro da API de Lancamentos");
        }

        private async Task ProcessBatchAsync(CancellationToken cancelationToken)
        {
            long currentCount = await _redisDb.ListLengthAsync(_redisBatchKey);
            if (currentCount == 0)
            {
                _logger.LogDebug("RedisBatchProcessor - Nenhuma mensagem no buffer Redis");
                return;
            }

            _logger.LogInformation("RedisBatchProcessor - Verificando mensagens no buffer Redis. Contagem atual: {Count}. Tamanho do lote: {BatchSize}", currentCount, _batchSize);

            var messages = await _redisDb.ListRangeAsync(_redisBatchKey, 0, _batchSize - 1);

            if (messages.Length > 0)
            {
                _logger.LogInformation("RedisBatchProcessor - Processando um lote de {Count} mensagens do Redis", messages.Length);

                try
                {
                    using var connection = _rabbitMqFactory.CreateConnectionAsync().GetAwaiter().GetResult();
                    using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                    await channel.QueueDeclareAsync(queue: _rabbitMqQueueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var batch = new List<string>();
                    foreach (var message in messages)
                    {
                        batch.Add(message.ToString());
                    }

                    var batchJson = JsonSerializer.Serialize(batch);
                    var body = Encoding.UTF8.GetBytes(batchJson);

                    var properties = new BasicProperties
                    {
                        Persistent = true // Mensagem fica persistente no disco
                    };
                    await channel.BasicPublishAsync(exchange: "",
                                         routingKey: _rabbitMqQueueName,
                                         mandatory: false,
                                         basicProperties: properties,
                                         body: body,
                                         cancellationToken: cancelationToken);

                    _logger.LogInformation("RedisBatchProcessor - Lote de {Count} mensagens enviado para o RabbitMQ", messages.Length);

                    await _redisDb.ListTrimAsync(_redisBatchKey, messages.Length, -1);
                    _logger.LogInformation("RedisBatchProcessor - {Count} mensagens removidas do buffer Redis", messages.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RedisBatchProcessor - Erro ao processar lote do Redis ou enviar para o RabbitMQ");
                }
            }
        }
    }
}
