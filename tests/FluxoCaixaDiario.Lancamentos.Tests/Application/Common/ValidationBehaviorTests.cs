using FluentAssertions;
using FluentValidation;
using FluxoCaixaDiario.Lancamentos.Application.Commands;
using FluxoCaixaDiario.Lancamentos.Application.Common;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using MediatR;
using Moq;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Application.Common
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_DeveChamarOProximoQuandoPassarPelaValidacao()
        {
            // Arrange
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            var mockNext = new Mock<RequestHandlerDelegate<bool>>();
            mockNext.Setup(m => m()).ReturnsAsync(true);

            var validators = new List<IValidator<RegisterTransactionCommand>> { new RegisterTransactionCommandValidator() };
            var behavior = new ValidationBehavior<RegisterTransactionCommand, bool>(validators);

            // Act
            var result = await behavior.Handle(command, CancellationToken.None, mockNext.Object);

            // Assert
            result.Should().BeTrue();
            mockNext.Verify(m => m(), Times.Once);
        }

        [Fact]
        public async Task Handle_DeveLancarUmaExcecaoQuandoAValidacaoFalhar()
        {
            // Arrange
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Data = null; 
            command.Valor = -10;

            var mockNext = new Mock<RequestHandlerDelegate<bool>>();
            mockNext.Setup(m => m()).ReturnsAsync(true);

            var validators = new List<IValidator<RegisterTransactionCommand>> { new RegisterTransactionCommandValidator() };
            var behavior = new ValidationBehavior<RegisterTransactionCommand, bool>(validators);

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await behavior.Handle(command, CancellationToken.None, mockNext.Object);
            });

            // Assert
            exception.Errors.Should().HaveCount(2);
            exception.Errors.Should().Contain(e => e.PropertyName == "Data" && e.ErrorMessage == "A data é obrigatória");
            exception.Errors.Should().Contain(e => e.PropertyName == "Valor" && e.ErrorMessage == "O valor deve ser maior que zero");

            mockNext.Verify(m => m(), Times.Never);
        }
    }
}
