using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using FluxoCaixaDiario.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Application.Commands;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using Moq;
using System;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Application.Commands
{
    public class RegisterTransactionCommandValidatorTests
    {
        private readonly RegisterTransactionCommandValidator _validator;

        public RegisterTransactionCommandValidatorTests()
        {
            _validator = new RegisterTransactionCommandValidator();
        }

        [Fact]
        public void DataNaoInformada_DeveResultarEmErro()
        {
            // Arrange
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Data = null;
            // Act
            var result = _validator.TestValidate(command);
            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Data).WithErrorMessage("A data é obrigatória");
        }

        [Fact]
        public void DataFutura_DeveResultarEmErro()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Data = DateTime.Today.Date.AddDays(1);
            var result = _validator.TestValidate(command);       
            result.ShouldHaveValidationErrorFor(c => c.Data).WithErrorMessage("A data do lançamento não pode ser futura");
        }

        [Fact]
        public void DataValida_NaoDeveResultarEmErro()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Data = DateTime.Today.Date;

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Data);
            result.IsValid.Should().BeTrue();
        }

        public void DescricaoUltrapassaLimitePermitido_DeveResultarEmErro()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Descricao = new string('A', 501);
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(c => c.Data).WithErrorMessage("A descrição não pode exceder 500 caracteres");
        }

        [Fact]
        public void ValorZerado_DeveResultarEmErro()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Valor = 0;
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(c => c.Valor).WithErrorMessage("O valor deve ser maior que zero");
        }

        [Theory]
        [InlineData((int)TransactionTypeEnum.Credit)]
        [InlineData((int)TransactionTypeEnum.Debit)]
        public void TipoTransacaoValido_NaoDeveResultarEmErro(int? tipo)
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Tipo = tipo;

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(c => c.Tipo);
        }

        [Fact]
        public void TipoTransacaoNulo_DeveResultarEmErro()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Tipo = null;
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(c => c.Tipo).WithErrorMessage("O tipo de lançamento é obrigatório");
        }

        [Fact]
        public void TipoTransacaoInvalido_DeveResultarEmErro()
        {
            var command = LancamentosGenerators.CreateRegisterTransactionCommand().Generate();
            command.Tipo = 2;
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(c => c.Tipo).WithErrorMessage("O tipo de lançamento deve ser Crédito(0) ou Débito(1)");
        }
    }
}
