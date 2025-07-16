using FluentAssertions;
using FluxoCaixaDiario.Domain.Enums;
using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using FluxoCaixaDiario.SaldoDiario.Tests.Generators;
using MediatR;
using Moq;
using Xunit;

namespace FluxoCaixaDiario.SaldoDiario.Application.Commands
{
    public class ProcessTransactionEventCommandHandlerUnitTests
    {
        private readonly Mock<IDailyBalanceRepository> _mockDailyBalanceRepository;
        private readonly ProcessTransactionEventCommandHandler _handler;

        public ProcessTransactionEventCommandHandlerUnitTests()
        {
            _mockDailyBalanceRepository = new Mock<IDailyBalanceRepository>();
            _handler = new ProcessTransactionEventCommandHandler(_mockDailyBalanceRepository.Object);
        }

        [Fact]
        public async Task Handle_DeveCriarNovoSaldoDiarioQuandoNaoEncontradoParaData()
        {
            // Arrange
            var dataTransacao = DateTime.Today;
            var valorTransacao = 100.50m;
            var tipoTransacao = TransactionTypeEnum.Credit;
            var command = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                               .WithTransactionDate(dataTransacao)
                                               .WithAmount(valorTransacao)
                                               .WithType(tipoTransacao)
                                               .Generate();

            _mockDailyBalanceRepository.Setup(r => r.GetByDateAsync(dataTransacao.Date))
                                       .ReturnsAsync((DailyBalance)null);

            // Act
            var resultado = await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(command, CancellationToken.None);

            // Assert
            resultado.Should().Be(Unit.Value);

            _mockDailyBalanceRepository.Verify(r => r.GetByDateAsync(dataTransacao.Date), Times.Once);
            _mockDailyBalanceRepository.Verify(r => r.UpsertAsync(It.Is<DailyBalance>(s =>
                s.Date == dataTransacao.Date &&
                s.TotalCredit == valorTransacao &&
                s.TotalDebit == 0 &&
                s.Balance == valorTransacao
            )), Times.Once);
        }

        [Theory]
        [InlineData(TransactionTypeEnum.Credit, 100, 50, 150, 50, 100)]
        [InlineData(TransactionTypeEnum.Debit, 100, 50, 100, 100, 0)] 
        public async Task Handle_DeveAtualizarSaldoDiarioExistenteComValoresCorretos(
            TransactionTypeEnum tipoTransacao,
            decimal creditoExistente,
            decimal debitoExistente,
            decimal creditoEsperado,
            decimal debitoEsperado,
            decimal saldoEsperado )
        {
            // Arrange
            var dataAtual = DateTime.Today.Date;
            var novoValor = 50.00m;
            var saldoDiarioExistente = SaldoDiarioGenerators.CreateDailyBalanceEntity()
                                                             .WithDate(dataAtual)
                                                             .WithTotalCredit(creditoExistente)
                                                             .WithTotalDebit(debitoExistente)
                                                             .WithBalance(creditoExistente - debitoExistente)
                                                             .Generate();

            var command = SaldoDiarioGenerators.CreateProcessTransactionEventCommand()
                                               .WithTransactionDate(dataAtual)
                                               .WithAmount(novoValor)
                                               .WithType(tipoTransacao)
                                               .Generate();

            _mockDailyBalanceRepository.Setup(r => r.GetByDateAsync(dataAtual.Date))
                                       .ReturnsAsync(saldoDiarioExistente);

            // Act
            var resultado = await ((IRequestHandler<ProcessTransactionEventCommand, Unit>)_handler).Handle(command, CancellationToken.None);

            // Assert
            resultado.Should().Be(Unit.Value);

            _mockDailyBalanceRepository.Verify(r => r.GetByDateAsync(dataAtual.Date), Times.Once);
            _mockDailyBalanceRepository.Verify(r => r.UpsertAsync(It.Is<DailyBalance>(s =>
                s.Date == dataAtual.Date &&
                s.TotalCredit == creditoEsperado &&
                s.TotalDebit == debitoEsperado &&
                s.Balance == saldoEsperado
            )), Times.Once);
        }
    }
}