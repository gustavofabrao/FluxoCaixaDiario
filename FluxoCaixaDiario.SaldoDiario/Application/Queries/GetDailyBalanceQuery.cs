using MediatR;
using System;

namespace FluxoCaixaDiario.SaldoDiario.Application.Queries
{
    public record GetDailyBalanceQuery(DateTime Date) : IRequest<GetDailyBalanceResult>;

    public record GetDailyBalanceResult(DateTime Date, decimal? TotalDebit, decimal? TotalCredit, decimal? Balance);
}