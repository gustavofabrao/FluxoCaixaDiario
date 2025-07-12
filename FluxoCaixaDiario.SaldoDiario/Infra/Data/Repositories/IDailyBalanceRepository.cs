using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories
{
    public interface IDailyBalanceRepository
    {
        Task<DailyBalance> GetByDateAsync(DateTime date);
        Task UpsertAsync(DailyBalance dailyBalance);
    }
}