using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Services.IServices
{
    public interface IRedisMessageBuffer
    {
        Task AddMessageAsync<T>(string key, T message);
    }
}
