using FluxoCaixaDiario.SaldoDiario.Application.Commands;
using FluxoCaixaDiario.Domain.Events;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Infra.MessageBroker
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IChannel _channel;
        private string _queueName;
        private string _exchangeName;
        private string _routingKey;

        public RabbitMqConsumerService(ILogger<RabbitMqConsumerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            _queueName = _configuration["RabbitMQ:QueueName"];
            _exchangeName = _configuration["RabbitMQ:ExchangeName"];
            _routingKey = _configuration["RabbitMQ:RoutingKey"];

            InitializeRabbitMq();
        }
        private void InitializeRabbitMq()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"],
                    Port = int.Parse(_configuration["RabbitMQ:Port"])
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                // Declara a exchange
                _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true);

                // Declara a fila (durable: true persiste a fila no broker)
                _channel.QueueDeclareAsync(queue: _queueName,
                                     durable: true,
                                     exclusive: false, // Pode ser acessada por múltiplos consumidores
                                     autoDelete: false, // Não é excluída quando não há consumidores
                                     arguments: null);

                // Vincula a fila à exchange com a routing key
                _channel.QueueBindAsync(queue: _queueName,
                                   exchange: _exchangeName,
                                   routingKey: _routingKey);

                // Define o QoS (Quality of Service) para limitar o número de mensagens não confirmadas
                // que um consumidor recebe de uma vez. Ajuda a evitar sobrecarga.
                _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 5, global: false); // Prefetch de 5 mensagens

                _logger.LogInformation("RabbitMQ Consumer: Conectado, exchange e fila configuradas.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ Consumer: Erro ao inicializar RabbitMQ.");
                // Poderia adicionar lógica de retry aqui para tentar reconectar
                throw;
            }
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Permite que o método Received logo abaixo seja assíncrono, liberando o thread do Consumer enquanto operações de entrada e saída estão em andamento.
            var consumer = new AsyncEventingBasicConsumer(_channel); 

            _logger.LogInformation("RabbitMQ Consumer: Iniciando o consumo de mensagens da fila '{QueueName}'.", _queueName);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("RabbitMQ Consumer: Mensagem recebida. Delivery Tag: {DeliveryTag}", ea.DeliveryTag);

                try
                {
                    var transactionEvent = JsonSerializer.Deserialize<TransactionRegisteredEvent>(message);

                    if (transactionEvent != null)
                    {
                        // Cria um escopo para o serviço para que as dependências sejam resolvidas corretamente
                        // para cada mensagem processada. Essencial em BackgroundService.
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                            var command = new ProcessTransactionEventCommand(
                                transactionEvent.TransactionId,
                                transactionEvent.TransactionDate,
                                transactionEvent.Amount,
                                transactionEvent.Type
                            );
                            await mediator.Send(command, stoppingToken); // Passa o CancellationToken
                        }
                    }

                    // Confirma que a mensagem foi processada com sucesso
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("RabbitMQ Consumer: Mensagem com Delivery Tag {DeliveryTag} processada e ACK enviado.", ea.DeliveryTag);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "RabbitMQ Consumer: Erro de desserialização JSON para mensagem com Delivery Tag {DeliveryTag}. Mensagem: {Message}", ea.DeliveryTag, message);
                    // Não re-enfileira mensagem com JSON inválido para evitar loop infinito
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ Consumer: Erro ao processar mensagem com Delivery Tag {DeliveryTag}. Mensagem: {Message}", ea.DeliveryTag, message);
                    // Rejeita a mensagem e a re-enfileira para que possa ser reprocessada.
                    // Considere um limite de retries ou uma Dead Letter Queue (DLQ) para mensagens problemáticas.
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // Inicia o consumo da fila
            _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer); // autoAck: false é crucial para controle manual

            _logger.LogInformation("RabbitMQ Consumer: Consumo iniciado na fila '{QueueName}'.", _queueName);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _logger.LogInformation("RabbitMQ Consumer: Desligando...");
            try
            {
                _channel?.CloseAsync();
                _connection?.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ Consumer: Erro ao fechar conexão ou canal do RabbitMQ.");
            }
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}