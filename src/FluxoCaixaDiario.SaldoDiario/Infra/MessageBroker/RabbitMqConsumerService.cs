using FluxoCaixaDiario.SaldoDiario.Application.Commands;
using FluxoCaixaDiario.Shared.Events;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
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

                _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true);

                _channel.QueueDeclareAsync(queue: _queueName,
                                     durable: true,
                                     exclusive: false, // Pode ser acessada por múltiplos consumidores
                                     autoDelete: false, // Não é excluída quando não há consumidores
                                     arguments: null);

                _channel.QueueBindAsync(queue: _queueName,
                                   exchange: _exchangeName,
                                   routingKey: _routingKey);

                // Define o Quality of Service para limitar o número de mensagens não confirmadas que um consumidor recebe de uma vez (Ajuda a evitar sobrecarga)
                _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 5, global: false); // Prefetch de 5 mensagens

                _logger.LogInformation("RabbitMQ Consumer: Conectado, exchange e fila configuradas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ Consumer: Erro ao inicializar RabbitMQ");
                throw;
            }
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Método Received  abaixo seja assíncrono, liberando o thread do Consumer enquanto operações de entrada e saída estão em andamento
            var consumer = new AsyncEventingBasicConsumer(_channel);

            _logger.LogInformation("RabbitMQ Consumer: Iniciando o consumo de mensagens da fila '{QueueName}'", _queueName);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var mensagens = Encoding.UTF8.GetString(body);
                _logger.LogInformation("RabbitMQ Consumer: Mensagem recebida. Delivery Tag: {DeliveryTag}", ea.DeliveryTag);

                try
                {
                    // Recebe agora uma lista de mensagens vinda do Redis 
                    var transactionsBatch = new List<TransactionRegisteredEvent>();
                    var jsonStringsBatch = JsonSerializer.Deserialize<List<string>>(mensagens);  
                    foreach (var jsonString in jsonStringsBatch)
                    {
                        var transactionEvent = JsonSerializer.Deserialize<TransactionRegisteredEvent>(jsonString);
                        if (transactionEvent != null)
                        {
                            transactionsBatch.Add(transactionEvent);
                        }
                    }

                    if (transactionsBatch.Count > 0)
                    {
                        // Cria um escopo para o serviço para cada lote de processamento. Essencial em BackgroundService
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                            foreach (var transaction in transactionsBatch)
                            {
                                var command = new ProcessTransactionEventCommand()
                                {
                                    TransactionId = transaction.TransactionId,
                                    TransactionDate = transaction.TransactionDate,
                                    Amount = transaction.Amount,
                                    Type = transaction.Type
                                };

                                await mediator.Send(command, stoppingToken);
                                _logger.LogInformation("RabbitMQ Consumer: Lançamento processado com sucesso: {Lancamento}", transaction.ToString());

                            }
                        }
                    }

                    // Confirma que a mensagem foi processada com sucesso
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("RabbitMQ Consumer: Mensagens com Delivery Tag {DeliveryTag} processadas e ACK enviado", ea.DeliveryTag);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "RabbitMQ Consumer: Erro de desserialização JSON para as mensagens com Delivery Tag {DeliveryTag}. Mensagens: {mensagens}", ea.DeliveryTag, mensagens);
                    // Não re-enfileira mensagem com JSON inválido para evitar loop indesejado e infinito
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ Consumer: Erro ao processar as mensagens com Delivery Tag {DeliveryTag}. Mensagens: {mensagens}", ea.DeliveryTag, mensagens);
                    // Rejeita a mensagem e a re-enfileira para que possa ser reprocessada
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer); // autoAck: false controle manual

            _logger.LogInformation("RabbitMQ Consumer: Consumo iniciado na fila '{QueueName}'.", _queueName);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _logger.LogInformation("RabbitMQ Consumer: Desligando");
            try
            {
                _channel?.CloseAsync();
                _connection?.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ Consumer: Erro ao fechar conexão ou canal do RabbitMQ");
            }
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}