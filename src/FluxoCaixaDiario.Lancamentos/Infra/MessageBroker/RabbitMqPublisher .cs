using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Infra.MessageBroker
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IRabbitMqChannelAdapter _channel;

        public RabbitMqPublisher(IConfiguration configuration, 
            ILogger<RabbitMqPublisher> logger, 
            IConnectionFactory connectionFactory,
            IRabbitMqChannelAdapter channel = null)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            try
            {
                _connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
                if (channel == null)
                {
                    var realChannel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                    _channel = new RabbitMqChannelAdapter(realChannel);
                }
                else
                {
                    _channel = channel;
                }

                _logger.LogInformation("Conectado com o RabbitMQ e canal criado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro de conexão com o RabbitMQ");
                throw;
            }
        }

        public Task PublishAsync<T>(string exchangeName, string routingKey, T message)
        {
            try
            {
                _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic, 
                    durable: true, autoDelete: false, null, cancellationToken: CancellationToken.None);

                var body = JsonSerializer.SerializeToUtf8Bytes(message);
                var properties = new BasicProperties
                {
                    Persistent = true // Mensagem fica persistente no disco
                };

                _channel.BasicPublishAsync(exchange: exchangeName,
                                     routingKey: routingKey,
                                     mandatory: true,
                                     properties: properties,
                                     body: body,
                                     cancellationToken: CancellationToken.None);

                _logger.LogInformation("Mensagem publicada na Exchange '{ExchangeName}', Routing Key '{RoutingKey}'", exchangeName, routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem no RabbitMQ. {Message}", message);
                _channel.BasicNackAsync(0, false, true, CancellationToken.None); // Nack da mensagem, a colocando de volta na fila
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.CloseAsync(0, null, CancellationToken.None);
            _connection?.CloseAsync();
            _logger.LogInformation("Conexão e canal do RabbitMQ fechados");
            GC.SuppressFinalize(this);
        }
    }
}
