using GeekShopping.PaymentAPI.Messages;
using GeekShopping.PaymentAPI.RabbitMQSender;
using GeekShopping.PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace GeekShopping.PaymentAPI.MessageConsumer
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private const string QUEUE_NAME = "order-payment-process-queue";
        private IConnection _connection;
        private IModel _channel;
        private readonly IProcessPayment _processPayment;
        private readonly IRabbitMQMessageSender _rabbitMQMessageSender;
        
        public RabbitMQPaymentConsumer(IProcessPayment processPayment, IRabbitMQMessageSender rabbitMQMessageSender)
        {
            _processPayment = processPayment;
            _rabbitMQMessageSender = rabbitMQMessageSender;

            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection();

            _channel = _connection.CreateModel();

            _channel.QueueDeclare(QUEUE_NAME, false, false, false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (channel, eventArgs) =>
            {
                var content = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var payment = JsonSerializer.Deserialize<PaymentMessage>(content);
                ProcessPayment(payment).GetAwaiter().GetResult();

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(QUEUE_NAME, false, consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessPayment(PaymentMessage payment)
        {
            var result = _processPayment.PaymentProcessor();

            var paymentResult = new UpdatePaymentResultMessage
            {
                Status = result,
                OrderId = payment.OrderId,
                Email = payment.Email
            };

            try
            {
                _rabbitMQMessageSender.SendMessage(paymentResult, "order-payment-result-queue");
            }
            catch (Exception)
            {
                //Log
                throw;
            }
        }
    }
}
