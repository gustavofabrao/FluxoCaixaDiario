using FluentAssertions;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Context;
using FluxoCaixaDiario.SaldoDiario.Infra.Data.Repositories;
using FluxoCaixaDiario.SaldoDiario.Tests.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MySql;
using Xunit;

namespace FluxoCaixaDiario.SaldoDiario.Tests.Infra.Data.Repositories
{
    public class DailyBalanceRepositoryIntegrationTests : IAsyncLifetime
    {
        private readonly MySqlContainer _mySqlContainer;
        private MySQLContext _dbContext;
        private DailyBalanceRepository _repository;
        private Mock<ILogger<DailyBalanceRepository>> _mockLogger;

        public DailyBalanceRepositoryIntegrationTests()
        {
            _mySqlContainer = new MySqlBuilder()
                .WithPassword("Admin123!")
                .Build();

            _mockLogger = new Mock<ILogger<DailyBalanceRepository>>();
        }

        public async Task InitializeAsync()
        {
            await _mySqlContainer.StartAsync();

            var connectionString = _mySqlContainer.GetConnectionString();
            var optionsBuilder = new DbContextOptionsBuilder<MySQLContext>()
                .UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)
                )
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            _dbContext = new MySQLContext(optionsBuilder.Options);

            await _dbContext.Database.MigrateAsync();

            _repository = new DailyBalanceRepository(_dbContext, _mockLogger.Object);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _mySqlContainer.DisposeAsync();
        }

        [Fact] 
        public async Task GetByDateAsync_DeveRetornarSaldoDiarioQuandoEncontrado()
        {
            // Arrange
            var data = DateTime.Today.Date;
            var expectedDailyBalance = SaldoDiarioGenerators.CreateDailyBalanceEntity().WithDate(data).Generate();
            await _dbContext.DailyBalances.AddAsync(expectedDailyBalance);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(expectedDailyBalance).State = EntityState.Detached; 

            // Act
            var result = await _repository.GetByDateAsync(data);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact] 
        public async Task GetByDateAsync_DeveRetornarNuloQuandoNaoEncontrado()
        {
            // Arrange
            var data = DateTime.Today.AddDays(-1).Date;

            // Act
            var result = await _repository.GetByDateAsync(data);

            // Assert
            result.Should().BeNull();
        }

        [Fact] 
        public async Task UpsertAsync_DeveAdicionarSaldoDiarioQuandoNaoEncontrado()
        {
            // Arrange
            var novoSaldoDiario = SaldoDiarioGenerators.CreateDailyBalanceEntity().Generate();
            novoSaldoDiario.Date = DateTime.Today.Date;

            // Act
            await _repository.UpsertAsync(novoSaldoDiario);

            // Assert
            var storedBalance = await _dbContext.DailyBalances.AsNoTracking().FirstOrDefaultAsync(db => db.Date == novoSaldoDiario.Date);
            storedBalance.Should().NotBeNull();
        }

        [Fact] 
        public async Task UpsertAsync_DeveAtualizarSaldoDiarioQuandoEncontrado()
        {
            // Arrange
            var saldoDiarioExistente = SaldoDiarioGenerators.CreateDailyBalanceEntity().Generate();
            saldoDiarioExistente.Date = DateTime.Today.Date;
            await _dbContext.DailyBalances.AddAsync(saldoDiarioExistente);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(saldoDiarioExistente).State = EntityState.Detached;

            var saldoDiarioAtualizado = SaldoDiarioGenerators.CreateDailyBalanceEntity()
                                                            .WithDate(saldoDiarioExistente.Date)
                                                            .WithTotalCredit(saldoDiarioExistente.TotalCredit)
                                                            .WithTotalDebit(saldoDiarioExistente.TotalDebit)
                                                            .Generate();
            saldoDiarioAtualizado.Balance = saldoDiarioAtualizado.TotalCredit - saldoDiarioAtualizado.TotalDebit; 

            // Act
            await _repository.UpsertAsync(saldoDiarioAtualizado);

            // Assert
            var storedBalance = await _dbContext.DailyBalances.FirstOrDefaultAsync(db => db.Date == saldoDiarioExistente.Date);
            storedBalance.Should().NotBeNull();
            storedBalance.TotalCredit.Should().Be(saldoDiarioAtualizado.TotalCredit);
            storedBalance.TotalDebit.Should().Be(saldoDiarioAtualizado.TotalDebit);
            storedBalance.Balance.Should().Be(saldoDiarioAtualizado.Balance);
        }
    }
}