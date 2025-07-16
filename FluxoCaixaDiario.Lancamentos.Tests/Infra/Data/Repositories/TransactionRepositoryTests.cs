using FluentAssertions;
using FluxoCaixaDiario.Shared.Entities;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Context;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Repositories;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Infra.Data.Repositories
{
    public class TransactionRepositoryTests : IDisposable
    {
        private readonly MySQLContext _context;
        private readonly TransactionRepository _repository;
        private readonly Mock<ILogger<TransactionRepository>> _mockLogger;

        public TransactionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MySQLContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Banco em memória para testes
                .Options;

            _context = new MySQLContext(options);
            _mockLogger = new Mock<ILogger<TransactionRepository>>();
            _repository = new TransactionRepository(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted(); // Limpa o banco em memória
            _context.Dispose();
        }


        [Fact]
        public async Task AddAsync_DeveAdicionarATransacaoELogarUmaInformacao()
        {
            // Arrange
            var newTransaction = LancamentosGenerators.CreateLancamentoEntity().Generate();

            // Act
            await _repository.AddAsync(newTransaction);

            // Assert
            var savedTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == newTransaction.Id);
            savedTransaction.Should().NotBeNull();
            savedTransaction.Id.Should().Be(newTransaction.Id);
            savedTransaction.Amount.Should().Be(newTransaction.Amount);

            // Por ultimo verifica se o log foi chamado com  nível de informação
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Transação {newTransaction.Id} adicionada com sucesso")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_DeveLancarExcecaoQuandoHouverErroDeAtualizacao()
        {
            // Arrange
            var transaction = LancamentosGenerators.CreateLancamentoEntity().Generate();

            // Simula um erro de chave primária duplicada, adicionando a mesma transação duas vezes
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            _context.Entry(transaction).State = EntityState.Detached; // Faz o Detach para adicionar novamente 

            // Act + Assert
            await _repository.Invoking(r => r.AddAsync(transaction))
                             .Should().ThrowAsync<ApplicationException>()
                             .WithMessage("Ocorreu um erro inesperado ao processar a transação");

            // Por ultimo verifica se o log foi chamado com  nível de erro
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Erro inesperado ao adicionar transação {transaction.Id}")),
                    It.IsAny<ArgumentException>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarTransacaoQuandoEncontrada()
        {
            // Arrange
            var existingTransaction = LancamentosGenerators.CreateLancamentoEntity().Generate();
            _context.Transactions.Add(existingTransaction);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(existingTransaction.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(existingTransaction.Id);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Never);
        }

        [Fact]
        public async Task GetbyIdAsync_DeveRetornarNuloELogarAvisoQuandoNaoEncontrada()
        {
            var nonExistentId = Guid.NewGuid();

            var result = await _repository.GetByIdAsync(nonExistentId);

            result.Should().BeNull();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Transação de Id {nonExistentId} não encontrada")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}