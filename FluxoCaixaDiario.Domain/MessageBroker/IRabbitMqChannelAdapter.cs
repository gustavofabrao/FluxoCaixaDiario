using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Domain.MessageBroker
{
    public interface IRabbitMqChannelAdapter : IDisposable
    {
        Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments, CancellationToken cancellationToken);

        Task BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
            where TProperties : IAmqpHeader, IReadOnlyBasicProperties;

        Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken);
        Task CloseAsync(ushort replyCode, string replyText, CancellationToken cancellationToken);

        Task QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments, CancellationToken cancellationToken);
        Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments, CancellationToken cancellationToken);
        Task BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken);

        IChannel GetUnderlyingChannel();
    }
}
