using System;

namespace FluxoCaixaDiario.Lancamentos.Infra.MessageBroker
{
    public class RabbitMqMessageDto
    {
        public Guid TransactionId { get; set; }
        public string EventType { get; set; }
        public object Payload { get; set; }
    }
}
