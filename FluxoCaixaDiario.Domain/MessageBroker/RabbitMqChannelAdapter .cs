using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;


namespace FluxoCaixaDiario.Domain.MessageBroker
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

        public async Task QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            await Task.Run(() => _channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments), cancellationToken);
        }

        public async Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            await Task.Run(() => _channel.QueueBindAsync(queue, exchange, routingKey, arguments), cancellationToken);
        }

        public async Task BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
        {
            await _channel.BasicAckAsync(deliveryTag, multiple, cancellationToken);
        }

        public IChannel GetUnderlyingChannel() => _channel;

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
