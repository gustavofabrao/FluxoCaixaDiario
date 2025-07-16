using FluentAssertions;
using FluxoCaixaDiario.Domain.Enums;
using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Context;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using FluxoCaixaDiario.SaldoDiario.Tests.Generators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Crypto;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FluxoCaixaDiario.SaldoDiario.Application.Commands
{
    public class ProcessTransactionEventCommandHandlerIntegrationTests : IDisposable
    {
        private MySQLContext _context;
        private DailyBalanceRepository _dailyBalanceRepository;
        private ProcessTransactionEventCommandHandler _handler;
        private readonly Mock<ILogger<DailyBalanceRepository>> _mockLogger;

        public ProcessTransactionEventCommandHandlerIntegrationTests()
        { 
            var options = new DbContextOptionsBuilder<MySQLContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MySQLContext(options);
            _mockLogger = new Mock<ILogger<DailyBalanceRepository>>();
            _dailyBalanceRepository = new DailyBalanceRepository(_context, _mockLogger.Object);

            // Instancia o handler  real do repositório
            _handler = new ProcessTransactionEventCommandHandler(_dailyBalanceRepository);
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted(); 
            _context.Dispose();
        }
         
        [Fact]
        public async Task Handle_DeveCriarNovoSaldoDiarioComCreditoQuandoNaoExiste()
        {
            // Arrange
            var dataTransacao = DateTime.Today.Date;
            var command = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                               .WithTransactionDate(dataTransacao)
                                               .WithAmount(100M)
                                               .WithType(TransactionTypeEnum.Credit)
                                               .Generate();

            // Act
            await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(command, CancellationToken.None);

            // Assert
            var storedBalance = await _context.DailyBalances.AsNoTracking().FirstOrDefaultAsync(db => db.Date == dataTransacao);

            storedBalance.Should().NotBeNull();
            storedBalance.TotalCredit.Should().Be(command.Amount);
            storedBalance.TotalDebit.Should().Be(0M);
            storedBalance.Balance.Should().Be(command.Amount);
            storedBalance.Date.Should().Be(dataTransacao);
        }

        [Fact]
        public async Task Handle_DeveCriarNovoSaldoDiarioComDebitoQuandoNaoExiste()
        {
            // Arrange
            var dataTransacao = DateTime.Today.Date;
            var command = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                               .WithTransactionDate(dataTransacao)
                                               .WithAmount(50M)
                                               .WithType(TransactionTypeEnum.Debit)
                                               .Generate();

            // Act
            await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(command, CancellationToken.None);

            // Assert
            var storedBalance = await _context.DailyBalances.AsNoTracking().FirstOrDefaultAsync(db => db.Date == dataTransacao);

            storedBalance.Should().NotBeNull();
            storedBalance.TotalCredit.Should().Be(0M);
            storedBalance.TotalDebit.Should().Be(command.Amount);
            storedBalance.Balance.Should().Be(-command.Amount); // Checa saldo negativo
            storedBalance.Date.Should().Be(dataTransacao);
        }

        [Fact]
        public async Task Handle_DeveAtualizarSaldoDiarioExistenteComCredito()
        {
            // Arrange
            var dataTransacao = DateTime.Today.Date;
            var saldoInicial = new DailyBalance
            {
                Date = dataTransacao,
                TotalCredit = 500M,
                TotalDebit = 100M,
                Balance = 400M
            };
            await _context.DailyBalances.AddAsync(saldoInicial);
            await _context.SaveChangesAsync();
            _context.Entry(saldoInicial).State = EntityState.Detached;

            var command = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                               .WithTransactionDate(dataTransacao)
                                               .WithAmount(150M)
                                               .WithType(TransactionTypeEnum.Credit)
                                               .Generate();

            // Act
            await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(command, CancellationToken.None);

            // Assert
            var saldoAtualizado = await _context.DailyBalances.AsNoTracking().FirstOrDefaultAsync(db => db.Date == dataTransacao);

            saldoAtualizado.Should().NotBeNull();
            saldoAtualizado.TotalCredit.Should().Be(saldoInicial.TotalCredit + command.Amount);
            saldoAtualizado.TotalDebit.Should().Be(saldoInicial.TotalDebit);
            saldoAtualizado.Balance.Should().Be(saldoAtualizado.TotalCredit - saldoAtualizado.TotalDebit);
            saldoAtualizado.Date.Should().Be(dataTransacao);
        }

        [Fact]
        public async Task Handle_DeveAtualizarSaldoDiarioExistenteComDebito()
        {
            // Arrange
            var dataTransacao = DateTime.Today.Date;
            var saldoInicial = new DailyBalance
            {
                Date = dataTransacao,
                TotalCredit = 500M,
                TotalDebit = 100M,
                Balance = 400M
            };
            await _context.DailyBalances.AddAsync(saldoInicial);
            await _context.SaveChangesAsync();
            _context.Entry(saldoInicial).State = EntityState.Detached;

            var command = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                               .WithTransactionDate(dataTransacao)
                                               .WithAmount(200M)
                                               .WithType(TransactionTypeEnum.Debit)
                                               .Generate();

            // Act
            await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(command, CancellationToken.None);

            // Assert
            var saldoAtualizado = await _context.DailyBalances.AsNoTracking().FirstOrDefaultAsync(db => db.Date == dataTransacao);

            saldoAtualizado.Should().NotBeNull();
            saldoAtualizado.TotalCredit.Should().Be(saldoInicial.TotalCredit); 
            saldoAtualizado.TotalDebit.Should().Be(saldoInicial.TotalDebit + command.Amount); 
            saldoAtualizado.Balance.Should().Be(saldoAtualizado.TotalCredit - saldoAtualizado.TotalDebit); 
            saldoAtualizado.Date.Should().Be(dataTransacao);
        }

        [Fact]
        public async Task Handle_DeveAtualizarSaldoDiarioComCreditoEDebito()
        {
            // Arrange
            var dataTransacao = DateTime.Today.Date;
            var saldoInicial = new DailyBalance
            {
                Date = dataTransacao,
                TotalCredit = 500M,
                TotalDebit = 100M,
                Balance = 400M
            };
            await _context.DailyBalances.AddAsync(saldoInicial);
            await _context.SaveChangesAsync();
            _context.Entry(saldoInicial).State = EntityState.Detached;

            // Primeiro, um crédito
            var commandCredito = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                                    .WithTransactionDate(dataTransacao)
                                                    .WithAmount(50M)
                                                    .WithType(TransactionTypeEnum.Credit)
                                                    .Generate();
            await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(commandCredito, CancellationToken.None);

            // Em seguida, um débito
            var commandDebito = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                                   .WithTransactionDate(dataTransacao)
                                                   .WithAmount(25M)
                                                   .WithType(TransactionTypeEnum.Debit)
                                                   .Generate();
            await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(commandDebito, CancellationToken.None);

            // Assert
            var saldoFinal = await _context.DailyBalances.AsNoTracking().FirstOrDefaultAsync(db => db.Date == dataTransacao);

            saldoFinal.Should().NotBeNull();
            saldoFinal.TotalCredit.Should().Be(saldoInicial.TotalCredit + commandCredito.Amount); // 500 + 50 = 550
            saldoFinal.TotalDebit.Should().Be(saldoInicial.TotalDebit + commandDebito.Amount); // 100 + 25 = 125
            saldoFinal.Balance.Should().Be(saldoFinal.TotalCredit - saldoFinal.TotalDebit); // 550 - 125 = 425
            saldoFinal.Date.Should().Be(dataTransacao);
        }
    }
}