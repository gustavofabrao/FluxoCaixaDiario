using FluentAssertions;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Context;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Repositories;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using FluxoCaixaDiario.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MySql;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Infra.Data.Repositories
{
    public class TransactionRepositoryIntegrationTests : IAsyncLifetime
    {
        private readonly MySqlContainer _mySqlContainer; 
        private MySQLContext _dbContext;
        private TransactionRepository _transactionRepository;
        private Mock<ILogger<TransactionRepository>> _mockLogger;

        public TransactionRepositoryIntegrationTests()
        {
            _mySqlContainer = new MySqlBuilder()
                .WithPassword("Admin123!") 
                .Build();

            _mockLogger = new Mock<ILogger<TransactionRepository>>();
        }

        public async Task InitializeAsync()
        {
            await _mySqlContainer.StartAsync();

            var optionsBuilder = new DbContextOptionsBuilder<MySQLContext>()
                .UseMySql(
                    _mySqlContainer.GetConnectionString(),
                    ServerVersion.AutoDetect(_mySqlContainer.GetConnectionString())
                )
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            _dbContext = new MySQLContext(optionsBuilder.Options);

            await _dbContext.Database.MigrateAsync();

            _transactionRepository = new TransactionRepository(_dbContext, _mockLogger.Object);
        }

        public async Task DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _mySqlContainer.DisposeAsync();
        }

        [Fact]
        public async Task AddAsync_DeveAdicionarTransacaoAoBancoDeDados()
        {
            // Arrange
            var transaction = LancamentosGenerators.CreateLancamentoEntity().Generate();

            // Act
            await _transactionRepository.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            // Assert
            var retrievedTransaction = await _dbContext.Set<Transaction>()
                                                       .AsNoTracking()
                                                       .FirstOrDefaultAsync(t => t.Id == transaction.Id);

            retrievedTransaction.Should().NotBeNull();
            retrievedTransaction.Should().BeEquivalentTo(transaction);
        }

        [Fact] 
        public async Task GetByIdAsync_DeveRetornarTransacaoQuandoEncontrada()
        {
            // Arrange
            var transaction = LancamentosGenerators.CreateLancamentoEntity().Generate();
            await _dbContext.Set<Transaction>().AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            // Act
            var retrievedTransaction = await _transactionRepository.GetByIdAsync(transaction.Id);

            // Assert
            retrievedTransaction.Should().NotBeNull();
            retrievedTransaction.Id.Should().Be(transaction.Id);
            retrievedTransaction.Description.Should().Be(transaction.Description);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarNuloQuandoNaoEncontrada()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var retrievedTransaction = await _transactionRepository.GetByIdAsync(nonExistentId);

            // Assert
            retrievedTransaction.Should().BeNull();
        }
    }
}