using FluentAssertions;
using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using FluxoCaixaDiario.Lancamentos.Tests.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.Tests.Infra.MessageBroker
{
    public class RabbitMqPublisherIntegrationTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbitMqContainer;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<RabbitMqPublisher>> _mockLogger;
        private RabbitMqPublisher _publisher;
        private ConnectionFactory _realConnectionFactory;

        public RabbitMqPublisherIntegrationTests()
        {
            _rabbitMqContainer = new RabbitMqBuilder()
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();

            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<RabbitMqPublisher>>();

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

            _realConnectionFactory = new ConnectionFactory
            {
                HostName = _rabbitMqContainer.Hostname,
                Port = _rabbitMqContainer.GetMappedPublicPort(5672),
                UserName = "guest",
                Password = "guest"
            };

            _publisher = new RabbitMqPublisher(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _realConnectionFactory);
        }

        public async Task DisposeAsync()
        {
            _publisher.Dispose();
            await _rabbitMqContainer.DisposeAsync();
        }

        [Fact]
        public async Task PublishAsync_DevePublicarAMensagemNoRabbitMQ()
        {
            // Arrange
            var uniqueId = Guid.NewGuid().ToString("N");
            var exchangeName = $"test_exchange_integration_{uniqueId}";
            var routingKey = $"test.key.integration.{uniqueId}";
            var message = LancamentosGenerators.CreateRabbitMqMessageDto().Generate();
            var serializedMessage = JsonSerializer.Serialize(message);

            IConnection consumerConnection = null;
            IChannel consumerChannel = null;
            string queueName = string.Empty;

            try
            {
                consumerConnection = await _realConnectionFactory.CreateConnectionAsync();
                consumerChannel = await consumerConnection.CreateChannelAsync();

                await consumerChannel.ExchangeDeclareAsync(
                    exchange: exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: CancellationToken.None
                );

                _mockLogger.Object.LogInformation($"TESTE DE INTEGRAÇÃO RABBITMQ - Consumidor - Exchange '{exchangeName}' declarada.");

                var queueDeclareResult = await consumerChannel.QueueDeclareAsync(
                    queue: "",
                    durable: false,
                    exclusive: true,
                    autoDelete: true,
                    arguments: null);
                queueName = queueDeclareResult.QueueName;

                _mockLogger.Object.LogInformation($"TESTE DE INTEGRAÇÃO RABBITMQ - Consumidor - Fila '{queueName}' declarada.");

                await consumerChannel.QueueBindAsync(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: routingKey);

                _mockLogger.Object.LogInformation($"TESTE DE INTEGRAÇÃO RABBITMQ - Consumidor - Fila '{queueName}' ligada à Exchange '{exchangeName}' com Routing Key '{routingKey}'.");

                var consumer = new AsyncEventingBasicConsumer(consumerChannel);
                var receivedMessageTaskCompletionSource = new TaskCompletionSource<string>();

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var receivedContent = Encoding.UTF8.GetString(body);
                    receivedMessageTaskCompletionSource.TrySetResult(receivedContent);
                    _mockLogger.Object.LogInformation($"TESTE DE INTEGRAÇÃO RABBITMQ - Consumidor - Mensagem recebida: {receivedContent}");
                    await Task.Yield();
                };

                _mockLogger.Object.LogInformation($"TESTE DE INTEGRAÇÃO RABBITMQ - Consumidor - Iniciado BasicConsume na fila '{queueName}'.");
                await consumerChannel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: true,
                    consumer: consumer);

                // Act
                _mockLogger.Object.LogInformation($"TESTE DE INTEGRAÇÃO RABBITMQ - Consumidor - Publicando mensagem na Exchange '{exchangeName}', Routing Key '{routingKey}'.");
                await _publisher.PublishAsync(exchangeName, routingKey, message);
                _mockLogger.Object.LogInformation($"[INTEGRATION TEST] Publicador: Chamada de publicação completada.");

                // Assert
                var result = await receivedMessageTaskCompletionSource.Task.WithTimeout(TimeSpan.FromSeconds(10));

                result.Should().Be(serializedMessage);

                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Mensagem publicada na Exchange '{exchangeName}', Routing Key '{routingKey}'")),
                        It.IsAny<Exception>(),
                        (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                    Times.Once);
            }
            finally
            {
                // Limpeza
                if (consumerChannel != null && consumerChannel.IsOpen)
                {
                    await consumerChannel.CloseAsync(200, "Fechamento normal do Channel");
                    consumerChannel.Dispose();
                }
                if (consumerConnection != null && consumerConnection.IsOpen)
                {
                    await consumerConnection.CloseAsync(200, "Fechamento normal do Consummer Connection");
                    consumerConnection.Dispose();
                }
            }
        }
    }
    
    public static class TaskExtensions
    {
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                throw new TimeoutException($"Operação sofreu time out depois de {timeout.TotalSeconds} segundos");
            }
            return await task;
        }
    }
}
