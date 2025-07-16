using FluentAssertions;
using FluxoCaixaDiario.SaldoDiario.Domain.Entities;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Context;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using FluxoCaixaDiario.SaldoDiario.Tests.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;
using Moq;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Linq.Expressions;
using Xunit;

namespace FluxoCaixaDiario.SaldoDiario.Tests.Infra.Data.Repositories
{
    public class DailyBalanceRepositoryUnitTests : IDisposable
    {
        private MySQLContext _context;
        private readonly Mock<ILogger<DailyBalanceRepository>> _mockLogger;
        private readonly Mock<IDailyBalanceRepository> _mockRepository;
        private DailyBalanceRepository _realRepositoryInstance;

        public DailyBalanceRepositoryUnitTests()
        {
            // DbContext com um banco de dados em memória
            var options = new DbContextOptionsBuilder<MySQLContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MySQLContext(options);
            _mockLogger = new Mock<ILogger<DailyBalanceRepository>>();
            _mockRepository = new Mock<IDailyBalanceRepository>();

            _realRepositoryInstance = new DailyBalanceRepository(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetByDateAsync_DeveRetornarSaldoDiarioQuandoEncontrado()
        {
            // Arrange
            var data = DateTime.Today.Date;
            var saldoExistente = SaldoDiarioGenerators.CreateDailyBalanceEntity().WithDate(data).Generate();

            await _context.DailyBalances.AddAsync(saldoExistente);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _realRepositoryInstance.GetByDateAsync(data);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(saldoExistente);

            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task GetByDateAsync_DeveRetornarNuloQuandoNaoEncontrado()
        {
            // Arrange
            var data = DateTime.Today.Date;

            // Act
            var result = await _realRepositoryInstance.GetByDateAsync(data);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task GetByDateAsync_DeveLogarErroELancarExcecaoQuandoHouverErroNoBancoDeDados()
        {
            // Arrange
            var data = DateTime.Today.Date;
            var dbException = new Exception("Simulação de erro no banco", new Exception("Erro interno de conexão."));

            // Criando um Mocks locais apenas para este teste
            var mockDbSet = new Mock<DbSet<DailyBalance>>();
            var mockContextForError = new Mock<MySQLContext>(new DbContextOptions<MySQLContext>());
            mockContextForError.Setup(c => c.DailyBalances).Returns(mockDbSet.Object);
            var repositoryLocal = new DailyBalanceRepository(mockContextForError.Object, _mockLogger.Object);

            // Act
            Func<Task> action = async () => await repositoryLocal.GetByDateAsync(data);

            // Assert
            await action.Should().ThrowAsync<ApplicationException>()
                        .WithMessage($"Ocorreu um erro ao buscar o saldo diário para {data.ToShortDateString()}");

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Erro ao buscar saldo diário para a data {data.ToShortDateString()}")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }


        [Fact]
        public async Task UpsertAsync_DeveAdicionarSaldoDiarioQuandoNaoEncontrado()
        {
            // Arrange
            var novoSaldoDiario = SaldoDiarioGenerators.CreateDailyBalanceEntity().Generate();

            // Act
            await _realRepositoryInstance.UpsertAsync(novoSaldoDiario);

            // Assert
            var saldoSalvo = await _context.DailyBalances.FindAsync(novoSaldoDiario.Date);
            saldoSalvo.Should().NotBeNull();
            saldoSalvo.Should().BeEquivalentTo(novoSaldoDiario);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Novo saldo diário para a data {novoSaldoDiario.Date.ToShortDateString()} adicionado")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task UpsertAsync_DeveAtualizarSaldoDiarioQuandoEncontrado()
        {
            // Arrange
            var saldoExistente = SaldoDiarioGenerators.CreateDailyBalanceEntity().Generate();
            await _context.DailyBalances.AddAsync(saldoExistente);
            await _context.SaveChangesAsync();

            var saldoAtualizado = SaldoDiarioGenerators.CreateDailyBalanceEntity()
                                                        .WithDate(saldoExistente.Date)
                                                        .WithTotalCredit(saldoExistente.TotalCredit + 50)
                                                        .Generate();

            // Act
            await _realRepositoryInstance.UpsertAsync(saldoAtualizado);

            // Assert
            var saldoVerificado = await _context.DailyBalances.FirstOrDefaultAsync(db => db.Date == saldoAtualizado.Date);
            saldoVerificado.Should().NotBeNull();
            saldoVerificado.TotalCredit.Should().Be(saldoAtualizado.TotalCredit);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Saldo diário para a data {saldoAtualizado.Date.ToShortDateString()} atualizado")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}