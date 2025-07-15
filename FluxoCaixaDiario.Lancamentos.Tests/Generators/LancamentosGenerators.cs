using Bogus;
using FluxoCaixaDiario.Domain.Entities;
using FluxoCaixaDiario.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Application.Commands;
using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;

namespace FluxoCaixaDiario.Lancamentos.Tests.Generators
{
    public static class LancamentosGenerators
    {
        public static Faker<RegisterTransactionCommand> CreateRegisterTransactionCommand()
        {
            return new Faker<RegisterTransactionCommand>()
                .RuleFor(c => c.Data, f => f.Date.Past(1))
                .RuleFor(c => c.Descricao, f => f.Name.FullName())
                .RuleFor(c => c.Valor, f => f.Finance.Amount(min: 1, max: 1000))
                .RuleFor(c => c.Tipo, f => f.PickRandom(0, 1));
        }

        public static Faker<Transaction> CreateLancamentoEntity()
        {
            return new Faker<Transaction>()
                .RuleFor(l => l.Id, f => f.Random.Guid())
                .RuleFor(l => l.Date, f => f.Date.Past(1))
                .RuleFor(l => l.Description, f => f.Name.FullName())
                .RuleFor(l => l.Amount, f => f.Finance.Amount(min: 1, max: 1000))
                .RuleFor(l => l.Type, f => f.PickRandom(TransactionTypeEnum.Credit, TransactionTypeEnum.Debit));
        }

        public static Faker<RabbitMqMessageDto> CreateRabbitMqMessageDto()
        {
            return new Faker<RabbitMqMessageDto>()
                .RuleFor(m => m.TransactionId, f => f.Random.Guid())
                .RuleFor(m => m.EventType, f => f.PickRandom("transacao_registrada"))
                .RuleFor(m => m.Payload, f => new {
                    Date = f.Date.Past(1),
                    Description = f.Name.FullName(),
                    Amount = f.Finance.Amount(min: 1, max: 1000),
                    Type = f.PickRandom(0, 1)
                });
        }
    }
}
