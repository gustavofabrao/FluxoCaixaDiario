using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories
{
    public class DailyBalanceRepository : IDailyBalanceRepository
    {
        private readonly MySQLContext _context;
        private readonly ILogger<DailyBalanceRepository> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public DailyBalanceRepository(MySQLContext context, ILogger<DailyBalanceRepository> logger)
        {
            _context = context;
            _logger = logger;
            _retryPolicy = Policy
                .Handle<DbUpdateConcurrencyException>()
                .Or<DbException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Tentativa {RetryCount} de retry devido a erro de concorrência ao banco de dados ao atualizar saldo diário.", retryCount);
                    });
        }

        public async Task<DailyBalance?> GetByDateAsync(DateTime date)
        {
            try
            {
                var dailyBalance = await _context.DailyBalances.FirstOrDefaultAsync(db => db.Date == date.Date);
                if (dailyBalance == null)
                {
                    _logger.LogDebug("Saldo diário para a data {Date} não encontrado", date.Date.ToShortDateString());
                }
                return dailyBalance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar saldo diário para a data {Date}", date.Date.ToShortDateString());
                throw new ApplicationException($"Ocorreu um erro ao buscar o saldo diário para {date.Date.ToShortDateString()}", ex);
            }
        }

        public async Task UpsertAsync(DailyBalance dailyBalance)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var existingBalance = await _context.DailyBalances.FindAsync(dailyBalance.Date);
                    if (existingBalance == null)
                    {
                        await _context.DailyBalances.AddAsync(dailyBalance);
                        _logger.LogInformation("Novo saldo diário para a data {Date} adicionado", dailyBalance.Date.ToShortDateString());
                    }
                    else
                    {
                        _context.Entry(existingBalance).CurrentValues.SetValues(dailyBalance);
                        _logger.LogInformation("Saldo diário para a data {Date} atualizado", dailyBalance.Date.ToShortDateString());
                    }
                    await _context.SaveChangesAsync();
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Erro de concorrência ao atualizar saldo diário para a data {Date}", dailyBalance.Date.ToShortDateString());
                throw new ApplicationException("Erro de concorrência ao atualizar o saldo diário. Por favor, tente novamente.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao fazer a peristência ao banco de dados do saldo diário para a data {Date}.", dailyBalance.Date.ToShortDateString());
                throw new ApplicationException("Não foi possível salvar o saldo diário devido a um erro no banco de dados.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao fazer upsert do saldo diário para a data {Date}.", dailyBalance.Date.ToShortDateString());
                throw new ApplicationException("Ocorreu um erro inesperado ao salvar o saldo diário.", ex);
            }
        }
    }
}