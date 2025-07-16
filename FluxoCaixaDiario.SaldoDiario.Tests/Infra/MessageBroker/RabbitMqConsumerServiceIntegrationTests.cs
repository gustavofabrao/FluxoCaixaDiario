using FluentAssertions;
using FluxoCaixaDiario.Domain.Enums;
using FluxoCaixaDiario.Domain.Events;
using FluxoCaixaDiario.Domain.Helpers;
using FluxoCaixaDiario.SaldoDiario.Application.Commands;
using FluxoCaixaDiario.SaldoDiario.Infra.MessageBroker;
using FluxoCaixaDiario.SaldoDiario.Tests.Generators;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace FluxoCaixaDiario.SaldoDiario.Tests.Infra.MessageBroker
{
    public class RabbitMqConsumerServiceIntegrationTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer;
        private readonly Mock<ILogger<RabbitMqConsumerService>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IConfiguration> _mockConfiguration;

        private RabbitMqConsumerService _consumerService;
        private IConnection _publisherConnection;
        private IChannel _publisherChannel;

        private string _testQueueName;
        private string _testExchangeName;
        private string _testRoutingKey;

        public RabbitMqConsumerServiceIntegrationTests()
        {
            _rabbitMqContainer = new RabbitMqBuilder()
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();

            _mockLogger = new Mock<ILogger<RabbitMqConsumerService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockMediator = new Mock<IMediator>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                                .Returns(_mockServiceScopeFactory.Object);
            _mockServiceScopeFactory.Setup(ssf => ssf.CreateScope())
                                    .Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(ss => ss.ServiceProvider)
                             .Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IMediator)))
                                .Returns(_mockMediator.Object);

            // Nomes únicos para cada execução de teste para evitar concorrencia entre testes
            var uniqueId = Guid.NewGuid().ToString("N");
            _testQueueName = $"test_queue_{uniqueId}";
            _testExchangeName = $"test_exchange_{uniqueId}";
            _testRoutingKey = $"test.key.{uniqueId}";

            _mockConfiguration.Setup(c => c["RabbitMQ:QueueName"]).Returns(_testQueueName);
            _mockConfiguration.Setup(c => c["RabbitMQ:ExchangeName"]).Returns(_testExchangeName);
            _mockConfiguration.Setup(c => c["RabbitMQ:RoutingKey"]).Returns(_testRoutingKey);

            _mockConfiguration.Setup(c => c["RabbitMQ:HostName"]).Returns("localhost");
            _mockConfiguration.Setup(c => c["RabbitMQ:UserName"]).Returns("guest");
            _mockConfiguration.Setup(c => c["RabbitMQ:Password"]).Returns("guest");
            _mockConfiguration.Setup(c => c["RabbitMQ:Port"]).Returns("5672");
        }

        public async Task InitializeAsync()
        {
            await _rabbitMqContainer.StartAsync();

            _mockConfiguration.Setup(c => c["RabbitMQ:HostName"]).Returns(_rabbitMqContainer.Hostname);
            _mockConfiguration.Setup(c => c["RabbitMQ:Port"]).Returns(_rabbitMqContainer.GetMappedPublicPort(5672).ToString());

            _consumerService = new RabbitMqConsumerService(
                _mockLogger.Object,
                _mockServiceProvider.Object,
                _mockConfiguration.Object
            );

            // Configura uma conexão e canal
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqContainer.Hostname,
                Port = _rabbitMqContainer.GetMappedPublicPort(5672),
                UserName = "guest",
                Password = "guest"
            };
            _publisherConnection = await factory.CreateConnectionAsync();
            _publisherChannel = await _publisherConnection.CreateChannelAsync();

            // Declara a exchange e fila que o consumer ficará ouvindo  
            await _publisherChannel.ExchangeDeclareAsync(exchange: _testExchangeName, type: ExchangeType.Topic, durable: true);
            await _publisherChannel.QueueDeclareAsync(queue: _testQueueName, durable: true, exclusive: false, autoDelete: false);
            await _publisherChannel.QueueBindAsync(queue: _testQueueName, exchange: _testExchangeName, routingKey: _testRoutingKey);
        }

        public async Task DisposeAsync()
        {
            // Garante que o Consummer é parado e seus recursos liberados
            if (_consumerService != null)
            {
                await _consumerService.StopAsync(CancellationToken.None);
                _consumerService.Dispose();
            }

            if (_publisherChannel != null && _publisherChannel.IsOpen)
            {
                await _publisherChannel.CloseAsync();
                _publisherChannel.Dispose();
            }
            if (_publisherConnection != null && _publisherConnection.IsOpen)
            {
                await _publisherConnection.CloseAsync();
                _publisherConnection.Dispose();
            }

            await _rabbitMqContainer.DisposeAsync();
        }

        private async Task PublishMessage(TransactionRegisteredEvent messageEvent)
        {
            var message = System.Text.Json.JsonSerializer.Serialize(messageEvent);
            var body = Encoding.UTF8.GetBytes(message);
            await _publisherChannel.BasicPublishAsync(
                exchange: _testExchangeName,
                routingKey: _testRoutingKey,
                body: body,
                cancellationToken: CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_DeveConsumirMensagemEEnviarComandoAoMediator()
        {
            // Arrange
            var transactionEvent = SaldoDiarioGenerators.CreateTransactionRegisteredEvent().Generate();
            transactionEvent.Amount = 300M;
            transactionEvent.Type = TransactionTypeEnum.Credit;
            transactionEvent.TransactionDate = DateTime.Today.Date;

            // Configura o Mediator para simular sucesso e capturar o comando enviado
            ProcessTransactionEventCommand capturedCommand = null;
            _mockMediator.Setup(m => m.Send(It.IsAny<ProcessTransactionEventCommand>(), It.IsAny<CancellationToken>()))
                         .Callback<IRequest<Unit>, CancellationToken>((cmd, token) => capturedCommand = cmd as ProcessTransactionEventCommand) ///
                         .Returns(Task.FromResult(Unit.Value));

            // Para saber quando o ACK foi enviado (penvio bem-sucedido)
            var ackReceivedTcs = new TaskCompletionSource<bool>();
            _mockLogger.Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("processada e ACK enviado")),
                null,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(() => ackReceivedTcs.TrySetResult(true));

            // Act
            await _consumerService.StartAsync(CancellationToken.None); // Inicia o Consummer
            await PublishMessage(transactionEvent); // Publica a mensagem 

            // Aguarda até que o ACK seja logado, com um timeout
            await ackReceivedTcs.Task.WithTimeout(TimeSpan.FromSeconds(10));

            // Assert
            _mockMediator.Verify(m => m.Send(
                It.Is<ProcessTransactionEventCommand>(cmd =>
                    cmd.TransactionId == transactionEvent.TransactionId &&
                    cmd.Amount == transactionEvent.Amount &&
                    cmd.Type == transactionEvent.Type &&
                    cmd.TransactionDate == transactionEvent.TransactionDate),
                It.IsAny<CancellationToken>()),
                Times.Once);

            capturedCommand.Should().NotBeNull();
            capturedCommand.TransactionId.Should().Be(transactionEvent.TransactionId);
            capturedCommand.Amount.Should().Be(transactionEvent.Amount);
            capturedCommand.Type.Should().Be(transactionEvent.Type);

            // Verifica logs do Consummer
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("RabbitMQ Consumer: Conectado, exchange e fila configuradas")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"RabbitMQ Consumer: Iniciando o consumo de mensagens da fila '{_testQueueName}'")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Mensagem recebida. Delivery Tag:")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DeveLogarErroNackEComRequeueFalseQuandoJsonForInvalido()
        {
            // Arrange
            var invalidJsonMessage = "{ \"invalid\": \"json\"";
            var body = Encoding.UTF8.GetBytes(invalidJsonMessage);

            var nackReceivedTcs = new TaskCompletionSource<bool>();
            _mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro de desserialização JSON")),
                It.IsAny<JsonException>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(() => nackReceivedTcs.TrySetResult(true));

            // Act
            await _consumerService.StartAsync(CancellationToken.None);
            // Publica a mensagem  no RabbitMQ
            await _publisherChannel.BasicPublishAsync(
                exchange: _testExchangeName,
                routingKey: _testRoutingKey, 
                body: body,
                cancellationToken: CancellationToken.None);

            // Aguarda o NACK ser logado
            await nackReceivedTcs.Task.WithTimeout(TimeSpan.FromSeconds(10));

            // Assert
            // Verifica que o Mediator Nunca foi chamado
            _mockMediator.Verify(m => m.Send(It.IsAny<ProcessTransactionEventCommand>(), It.IsAny<CancellationToken>()), Times.Never);

            // Verifica o log de erro de desserialização
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro de desserialização JSON para mensagem com Delivery Tag")),
                    It.IsAny<JsonException>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DeveLogarErroComNackERequeueTrueQuandoMediatorFalha()
        {
            // Arrange
            var transactionEvent = SaldoDiarioGenerators.CreateTransactionRegisteredEvent().Generate();
            var jsonMessage = JsonSerializer.Serialize(transactionEvent);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var mediatorException = new Exception("Erro simulado do Mediator.");

            var invocationCount = 0;
            _mockMediator.Setup(m => m.Send(It.IsAny<ProcessTransactionEventCommand>(), It.IsAny<CancellationToken>()))
                         .Returns(() =>
                         {
                             invocationCount++;
                             if (invocationCount == 1) // Na primeira tentativa, lançar a exceção
                             {
                                 throw mediatorException;
                             }
                             // Para as invocações posteriores, devido ao requeue=true e evitarmos loop infinito retornamos sucesso
                             return Task.FromResult(Unit.Value);
                         });

            var nackReceivedTcs = new TaskCompletionSource<bool>();
            _mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao processar mensagem com Delivery Tag")),
                mediatorException,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(() => nackReceivedTcs.TrySetResult(true));

            // Act
            await _consumerService.StartAsync(CancellationToken.None);
            await PublishMessage(transactionEvent);

            await nackReceivedTcs.Task.WithTimeout(TimeSpan.FromSeconds(10));

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao processar mensagem com Delivery Tag")),
                    mediatorException,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);

            _mockMediator.Verify(m => m.Send(
                It.IsAny<ProcessTransactionEventCommand>(),
                It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());
        }
    }
}