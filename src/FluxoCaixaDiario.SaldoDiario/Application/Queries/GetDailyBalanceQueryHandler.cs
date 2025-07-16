using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Application.Queries
{
    public class GetDailyBalanceQueryHandler : IRequestHandler<GetDailyBalanceQuery, GetDailyBalanceResult>
    {
        private readonly IDailyBalanceRepository _dailyBalanceRepository;

        public GetDailyBalanceQueryHandler(IDailyBalanceRepository dailyBalanceRepository)
        {
            _dailyBalanceRepository = dailyBalanceRepository;
        }

        public async Task<GetDailyBalanceResult> Handle(GetDailyBalanceQuery request, CancellationToken cancellationToken)
        {
            var dailyBalance = await _dailyBalanceRepository.GetByDateAsync(request.Date.Date);
            return new GetDailyBalanceResult(request.Date.Date, dailyBalance?.TotalDebit, dailyBalance?.TotalCredit, dailyBalance?.Balance);
        }
    }
}