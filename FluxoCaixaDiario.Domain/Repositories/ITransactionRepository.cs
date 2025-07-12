using FluxoCaixaDiario.Domain.Entities;

namespace FluxoCaixaDiario.Domain.Repositories
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(Guid id);
    }
}
