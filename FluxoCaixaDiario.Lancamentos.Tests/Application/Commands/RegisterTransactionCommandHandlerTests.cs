using FluentAssertions;
using FluxoCaixaDiario.Domain.Entities;
using FluxoCaixaDiario.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Application.Commands;
using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Application.Commands
{
    public class RegisterTransactionCommandHandlerTests
    {
        private readonly Mock<ITransactionRepository> _mockLancamentoRepository;
        private readonly Mock<IRabbitMqPublisher> _mockMessagePublisher;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly RegisterTransactionCommandHandler _handler;

        public RegisterTransactionCommandHandlerTests()
        {
            _mockLancamentoRepository = new Mock<ITransactionRepository>();
            _mockMessagePublisher = new Mock<IRabbitMqPublisher>();
            _mockConfiguration = new Mock<IConfiguration>();
            _handler = new RegisterTransactionCommandHandler(_mockLancamentoRepository.Object,
                            _mockMessagePublisher.Object, 
                            _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_DeveAdicionarOLancamentoComSucesso()
        {
            // Arrange
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();

            _mockLancamentoRepository.Setup(repo => repo.AddAsync(It.IsAny<Transaction>()))
                                     .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockLancamentoRepository.Verify(repo => repo.AddAsync(It.IsAny<Transaction>()), Times.Once);
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_DeveLancarUmaExcecaoQuandoRepositorioFalhar()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();

            _mockLancamentoRepository.Setup(repo => repo.AddAsync(It.IsAny<Transaction>()))
                                     .ThrowsAsync(new InvalidOperationException("Erro simulado no repositório"));

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<InvalidOperationException>()
                          .WithMessage("Erro simulado no repositório");

            _mockLancamentoRepository.Verify(repo => repo.AddAsync(It.IsAny<Transaction>()), Times.Once);
        }
    }
}