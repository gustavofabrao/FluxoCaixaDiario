using Bogus;
using FluxoCaixaDiario.Domain.Entities;
using FluxoCaixaDiario.Domain.Enums;
using FluxoCaixaDiario.Domain.Events;
using FluxoCaixaDiario.SaldoDiario.Application.Commands;
using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Tests.Infra.MessageBroker;

namespace FluxoCaixaDiario.SaldoDiario.Tests.Generators
{
    public static class SaldoDiarioGenerators
    {
        public static Faker<ProcessTransactionEventCommand> CreateProcessTransactionEventCommand()
        {
            return new Faker<ProcessTransactionEventCommand>()
                .RuleFor(l => l.TransactionId, f => f.Random.Guid())
                .RuleFor(l => l.TransactionDate, f => f.Date.Recent(30).Date)
                .RuleFor(l => l.Amount, f => f.Finance.Amount(min: 1, max: 1000))
                .RuleFor(l => l.Type, f => f.PickRandom(TransactionTypeEnum.Credit, TransactionTypeEnum.Debit));
        }
        public static Faker<ProcessTransactionEventCommand> WithTransactionDate(this Faker<ProcessTransactionEventCommand> faker, DateTime date)
        {
            return faker.RuleFor(c => c.TransactionDate, date.Date);
        }
        public static Faker<ProcessTransactionEventCommand> WithAmount(this Faker<ProcessTransactionEventCommand> faker, decimal amount)
        {
            return faker.RuleFor(c => c.Amount, amount);
        }
        public static Faker<ProcessTransactionEventCommand> WithType(this Faker<ProcessTransactionEventCommand> faker, TransactionTypeEnum type)
        {
            return faker.RuleFor(c => c.Type, type);
        }


        public static Faker<DailyBalance> CreateDailyBalanceEntity()
        {
            return new Faker<DailyBalance>()
                .RuleFor(db => db.Date, f => f.Date.Past(1).Date)
                .RuleFor(db => db.TotalCredit, f => Math.Round(f.Random.Decimal(0, 10000), 2))
                .RuleFor(db => db.TotalDebit, f => Math.Round(f.Random.Decimal(0, 5000), 2))
                .RuleFor(db => db.Balance, (f, db) => db.TotalCredit - db.TotalDebit);
        }
        public static Faker<DailyBalance> WithDate(this Faker<DailyBalance> faker, DateTime date)
        {
            return faker.RuleFor(db => db.Date, date.Date);
        }
        public static Faker<DailyBalance> WithTotalCredit(this Faker<DailyBalance> faker, decimal totalCredit)
        {
            return faker.RuleFor(db => db.TotalCredit, totalCredit)
                        .RuleFor(db => db.Balance, (f, db) => db.TotalCredit - db.TotalDebit);
        }
        public static Faker<DailyBalance> WithTotalDebit(this Faker<DailyBalance> faker, decimal totalDebit)
        {
            return faker.RuleFor(db => db.TotalDebit, totalDebit)
                        .RuleFor(db => db.Balance, (f, db) => db.TotalCredit - db.TotalDebit);
        }
        public static Faker<DailyBalance> WithBalance(this Faker<DailyBalance> faker, decimal balance)
        {
            return faker.RuleFor(db => db.Balance, balance);
        }


        public static Faker<TransactionRegisteredEvent> CreateTransactionRegisteredEvent()
        {
            return new Faker<TransactionRegisteredEvent>()
                .RuleFor(t => t.TransactionId, f => f.Random.Guid())
                .RuleFor(t => t.TransactionDate, f => f.Date.Recent(30).Date) 
                .RuleFor(t => t.Amount, f => f.Finance.Amount(min: 10, max: 1000)) 
                .RuleFor(t => t.Type, f => f.PickRandom<TransactionTypeEnum>());
        }
        public static Faker<TransactionRegisteredEvent> WithTransactionId(this Faker<TransactionRegisteredEvent> faker, Guid transactionId)
        {
            return faker.RuleFor(t => t.TransactionId, transactionId);
        }
        public static Faker<TransactionRegisteredEvent> WithTransactionDate(this Faker<TransactionRegisteredEvent> faker, DateTime transactionDate)
        {
            return faker.RuleFor(t => t.TransactionDate, transactionDate.Date);
        }
        public static Faker<TransactionRegisteredEvent> WithAmount(this Faker<TransactionRegisteredEvent> faker, decimal amount)
        {
            return faker.RuleFor(t => t.Amount, amount);
        }
        public static Faker<TransactionRegisteredEvent> WithType(this Faker<TransactionRegisteredEvent> faker, TransactionTypeEnum type)
        {
            return faker.RuleFor(t => t.Type, type);
        }
    }
}
