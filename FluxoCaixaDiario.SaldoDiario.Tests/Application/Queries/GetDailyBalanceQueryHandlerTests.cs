using FluentAssertions;
using FluxoCaixaDiario.SaldoDiario.Application.Queries;
using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using FluxoCaixaDiario.SaldoDiario.Tests.Generators;
using Moq;
using Xunit;

namespace FluxoCaixaDiario.SaldoDiario.Tests.Application.Queries
{
    public class GetDailyBalanceQueryHandlerUnitTests
    {
        private readonly Mock<IDailyBalanceRepository> _mockDailyBalanceRepository;
        private readonly GetDailyBalanceQueryHandler _handler;

        public GetDailyBalanceQueryHandlerUnitTests()
        {
            _mockDailyBalanceRepository = new Mock<IDailyBalanceRepository>();
            _handler = new GetDailyBalanceQueryHandler(_mockDailyBalanceRepository.Object);
        }

        [Fact]
        public async Task Handle_DeveRetornarSaldoDiarioQuandoEncontrado()
        {
            // Arrange
            var date = DateTime.Today.Date;
            var expectedDailyBalance = SaldoDiarioGenerators.CreateDailyBalanceEntity().WithDate(date).Generate();

            _mockDailyBalanceRepository.Setup(r => r.GetByDateAsync(date))
                                       .ReturnsAsync(expectedDailyBalance);

            var query = new GetDailyBalanceQuery(date);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Date.Should().Be(expectedDailyBalance.Date);
            result.TotalCredit.Should().Be(expectedDailyBalance.TotalCredit);
            result.TotalDebit.Should().Be(expectedDailyBalance.TotalDebit);
            result.Balance.Should().Be(expectedDailyBalance.Balance);

            _mockDailyBalanceRepository.Verify(r => r.GetByDateAsync(date), Times.Once);
        }

        [Fact]
        public async Task Handle_DeveRetornarNuloTotaisESaldoQuandoNaoEncontrado()
        {
            // Arrange
            var date = DateTime.Today.AddDays(-1).Date;
            _mockDailyBalanceRepository.Setup(r => r.GetByDateAsync(date))
                                       .ReturnsAsync((DailyBalance)null); // Retorna nulo

            var query = new GetDailyBalanceQuery(date);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Date.Should().Be(date);
            result.TotalCredit.Should().BeNull();
            result.TotalDebit.Should().BeNull();
            result.Balance.Should().BeNull();

            _mockDailyBalanceRepository.Verify(r => r.GetByDateAsync(date), Times.Once);
        }
    }
}