using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using FluxoCaixaDiario.Shared.Enums;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Application.Commands
{
    public class ProcessTransactionEventCommandHandler : IRequestHandler<ProcessTransactionEventCommand>
    {
        private readonly IDailyBalanceRepository _dailyBalanceRepository;

        public ProcessTransactionEventCommandHandler(IDailyBalanceRepository dailyBalanceRepository)
        {
            _dailyBalanceRepository = dailyBalanceRepository;
        }

        async Task<Unit> IRequestHandler<ProcessTransactionEventCommand, Unit>.Handle(ProcessTransactionEventCommand request, CancellationToken cancellationToken)
        {
            var date = request.TransactionDate.Date;
            var dailyBalance = await _dailyBalanceRepository.GetByDateAsync(date);

            if (dailyBalance == null)
            {
                dailyBalance = new DailyBalance
                {
                    Date = date,
                    TotalCredit = 0,
                    TotalDebit = 0,
                    Balance = 0
                };
            }

            if (request.Type == TransactionTypeEnum.Credit)
            {
                dailyBalance.TotalCredit += request.Amount;
            }
            else
            {
                dailyBalance.TotalDebit += request.Amount;
            }

            dailyBalance.Balance = dailyBalance.TotalCredit - dailyBalance.TotalDebit;

            await _dailyBalanceRepository.UpsertAsync(dailyBalance);

            return Unit.Value;
        }
    }
}