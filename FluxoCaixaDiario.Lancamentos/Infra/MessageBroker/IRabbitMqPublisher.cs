using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Infra.MessageBroker
{
    public interface IRabbitMqPublisher
    {
        Task PublishAsync<T>(string exchangeName, string routingKey, T message);
    }
}
