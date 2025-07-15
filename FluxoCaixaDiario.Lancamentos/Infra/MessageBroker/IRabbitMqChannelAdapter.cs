using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Infra.MessageBroker
{
    public interface IRabbitMqChannelAdapter
    {
        Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments, CancellationToken cancellationToken);

        Task BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
            where TProperties : IAmqpHeader, IReadOnlyBasicProperties;

        Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken);
        Task CloseAsync(ushort replyCode, string replyText, CancellationToken cancellationToken);
    }
}
