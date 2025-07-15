using FluxoCaixaDiario.Domain.Entities;
using FluxoCaixaDiario.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Infra.Data.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly MySQLContext _context;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(MySQLContext context, ILogger<TransactionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(Transaction transaction)
        {
            try
            {
                await _context.Transactions.AddAsync(transaction);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Transação {TransactionId} adicionada com sucesso", transaction.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro na persitência para adicionar transação {TransactionId}", transaction.Id);
                throw new ApplicationException("Não foi possível salvar a transação devido a um erro no banco de dados", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao adicionar transação {TransactionId}", transaction.Id);
                throw new ApplicationException("Ocorreu um erro inesperado ao processar a transação", ex);
            }
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    _logger.LogWarning("Transação de Id {TransactionId} não encontrada", id);
                }
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar transação com Id {TransactionId}", id);
                throw new ApplicationException("Ocorreu um erro ao buscar a transação", ex);
            }
        }
    }
}