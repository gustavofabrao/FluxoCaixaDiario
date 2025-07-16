using FluxoCaixaDiario.Shared.Entities;

namespace FluxoCaixaDiario.Shared.Repositories
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(Guid id);
    }
}
