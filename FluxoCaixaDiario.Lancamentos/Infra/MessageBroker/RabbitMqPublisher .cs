using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Infra.MessageBroker
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = configuration["RabbitMQ:HostName"],
                    UserName = configuration["RabbitMQ:UserName"],
                    Password = configuration["RabbitMQ:Password"],
                    Port = int.Parse(configuration["RabbitMQ:Port"])
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                _logger.LogInformation("Conectado com o RabbitMQ e canal criado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro de conexão com o RabbitMQ");
                throw; // Relança a exceção para falhar o startup se a conexão for crítica
            }
        }

        public Task PublishAsync<T>(string exchangeName, string routingKey, T message)
        {
            try
            {
                // Declara a exchange (garante que ela exista). durable: true persiste a exchange
                // type: ExchangeType.Topic permite roteamento flexível com wildcards
                _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

                var body = JsonSerializer.SerializeToUtf8Bytes(message);

                // Fix: Replace the missing CreateBasicProperties method with an alternative approach
                var properties = new BasicProperties
                {
                    Persistent = true // Mensagem fica persistente no disco
                };

                _channel.BasicPublishAsync(exchange: exchangeName,
                                     routingKey: routingKey,
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: body);

                _logger.LogInformation("Mensagem publicada na Exchange '{ExchangeName}', Routing Key '{RoutingKey}'", exchangeName, routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem no RabbitMQ. {Message}", message);
                _channel.BasicNackAsync(0, false, true); // Nack da mensagem, a colocando de volta na fila
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            _logger.LogInformation("Conexão e canal do RabbitMQ fechados");
            GC.SuppressFinalize(this);
        }
    }
}
