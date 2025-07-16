using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System.Text.Json;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Infra.MessageBroker
{
    public class RabbitMqPublisherTests : IDisposable
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<RabbitMqPublisher>> _mockLogger;
        private readonly Mock<IConnectionFactory> _mockConnectionFactory;
        private readonly Mock<IConnection> _mockConnection;
        private readonly Mock<IRabbitMqChannelAdapter> _mockChannel;

        private RabbitMqPublisher _publisher;

        public RabbitMqPublisherTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<RabbitMqPublisher>>();
            _mockConnectionFactory = new Mock<IConnectionFactory>();
            _mockConnection = new Mock<IConnection>();
            _mockChannel = new Mock<IRabbitMqChannelAdapter>();

            _mockConfiguration.Setup(c => c["RabbitMQ:HostName"]).Returns("localhost");
            _mockConfiguration.Setup(c => c["RabbitMQ:UserName"]).Returns("guest");
            _mockConfiguration.Setup(c => c["RabbitMQ:Password"]).Returns("guest");
            _mockConfiguration.Setup(c => c["RabbitMQ:Port"]).Returns("5672");

            _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(_mockConnection.Object);

            _publisher = new RabbitMqPublisher(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockConnectionFactory.Object,
                _mockChannel.Object);
        }

        public void Dispose()
        {
            _publisher.Dispose();
            _mockChannel.Verify(a => a.CloseAsync(It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once); 
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Conexão e canal do RabbitMQ fechados")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public void Construtor_DeveLogarErroEArremessarExceção_QuandoFalharConexão()
        {
            // Arrange 
            _mockConnectionFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Erro de conexão simulada"));

            // Act + Asserts
            Assert.Throws<Exception>(() =>
            {
                var tempPublisher = new RabbitMqPublisher(_mockConfiguration.Object, _mockLogger.Object, _mockConnectionFactory.Object);
            });

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro de conexão com o RabbitMQ")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }


        [Fact]
        public async Task PublishAsync_DevePublicarMensagemComSucessoEPublicarMensagem()
        {
            // Arrange
            var exchangeName = "test_exchange";
            var routingKey = "test.key";
            var message = LancamentosGenerators.CreateRabbitMqMessageDto().Generate();
            var serializedBody = JsonSerializer.SerializeToUtf8Bytes(message);

            // Act
            await _publisher.PublishAsync(exchangeName, routingKey, message);

            // Assert
            _mockChannel.Verify(
                c => c.ExchangeDeclareAsync(
                    exchangeName, 
                    ExchangeType.Topic,
                    true,
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object>>(),  
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockChannel.Verify(
                c => c.BasicPublishAsync(
                    exchangeName,
                    routingKey,
                    true,
                    It.Is<BasicProperties>(p => p.Persistent),
                    It.Is<ReadOnlyMemory<byte>>(b => b.ToArray().SequenceEqual(serializedBody)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verifica se o logger foi chamado para logar a publicação
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Mensagem publicada na Exchange '{exchangeName}', Routing Key '{routingKey}'")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }


        [Fact]
        public async Task PublishAsync_DeveLogarErroERejeitarMensagemQuandoFalharPublicação()
        {
            // Arrange
            var exchangeName = "test_exchange";
            var routingKey = "test.key";
            var message = LancamentosGenerators.CreateRabbitMqMessageDto().Generate();

            // Mocka uma exceção ao tentar publicar
            _mockChannel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Simulação de erro de publicação"));

            // Act
            await _publisher.PublishAsync(exchangeName, routingKey, message);

            // Assert 
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao publicar mensagem no RabbitMQ.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);

            // Verifica se BasicNackAsync foi chamado
            _mockChannel.Verify(
                c => c.BasicNackAsync(0, false, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
