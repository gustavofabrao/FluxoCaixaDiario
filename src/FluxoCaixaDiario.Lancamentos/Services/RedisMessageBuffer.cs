using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using FluxoCaixaDiario.Lancamentos.Services.IServices;
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
    public class RedisMessageBuffer : IRedisMessageBuffer
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisMessageBuffer> _logger;

        public RedisMessageBuffer(IConnectionMultiplexer redis, ILogger<RedisMessageBuffer> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task AddMessageAsync<T>(string key, T message)
        {
            var serializedMessage = JsonSerializer.Serialize(message);
            await _database.ListRightPushAsync(key, serializedMessage);
            _logger.LogInformation("RedisMessageBuffer - Mensagem adicionada ao buffer Redis na chave {Key}", key);
        }
    }
}
