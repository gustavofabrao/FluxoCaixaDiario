using System.Threading.Tasks;

namespace FluxoCaixaDiario.MessageBus
{
    public interface IMessageBus
    {
        Task PublicMessage(BaseMessage message, string queueName);
    }
}
