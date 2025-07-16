using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;


namespace FluxoCaixaDiario.Lancamentos.Infra.MessageBroker
{
    public class RabbitMqChannelAdapter : IRabbitMqChannelAdapter
    {
        private readonly IChannel _channel;

        public RabbitMqChannelAdapter(IChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public async Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            await _channel.ExchangeDeclareAsync(exchange, type, durable, autoDelete, arguments, false, cancellationToken);
        }

        public async Task BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
            where TProperties : IAmqpHeader, IReadOnlyBasicProperties
        {
            await _channel.BasicPublishAsync(exchange, routingKey, mandatory, properties, body, cancellationToken);
        }

        public async Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
        {
            await _channel.BasicNackAsync(deliveryTag, multiple, requeue, cancellationToken);
        }

        public async Task CloseAsync(ushort replyCode, string replyText, CancellationToken cancellationToken)
        {
            await _channel.CloseAsync(replyCode, replyText, cancellationToken);
        }
    }
}
