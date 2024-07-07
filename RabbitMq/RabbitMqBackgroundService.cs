using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PreProcessor.RabbitMq
{
    public class PreProcessorBackgroundService : BackgroundService, IRabbitMqService, IRabbitMqBackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        private const string ProcessorQueueName = "ProcessorQueue";
        private const string FromWebCompBot = "PreProcessorQueue";

        public PreProcessorBackgroundService()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: ProcessorQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueDeclare(queue: FromWebCompBot, durable: true, exclusive: false, autoDelete: false, arguments: null);

            // Установка ограничения на количество непотвержденных сообщений
            _channel.BasicQos(0, 1, false);
        }

        public void SendMessageToQueue(string queueName, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
            Console.WriteLine($"[x] Отправлено {message} в {queueName}");
        }

        public void AcknowledgeMessage(ulong deliveryTag)
        {
            _channel.BasicAck(deliveryTag, false);
        }

        public void RejectMessage(ulong deliveryTag, bool requeue)
        {
            _channel.BasicNack(deliveryTag, false, requeue);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ProcessMessagesAsync(stoppingToken);
        }

        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var deserializedMessage = JsonSerializer.Deserialize<Message>(message);

                Console.WriteLine($"Получено сообщение: {message}");

                try
                {
                    // Обработка сообщения
                    Console.WriteLine($"Обработка сообщения с ID: {deserializedMessage.Id}");

                    deserializedMessage.Content = deserializedMessage.Content.ToLower();

                    SendMessageToQueue(ProcessorQueueName, JsonSerializer.Serialize(deserializedMessage));

                    // Подтверждение сообщения
                    AcknowledgeMessage(ea.DeliveryTag);
                    Console.WriteLine($"Сообщение подтверждено с deliveryTag: {ea.DeliveryTag}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла ошибка: {ex.Message}");
                    // Отклонение сообщения в случае ошибки
                    RejectMessage(ea.DeliveryTag, true);
                }
            };

            _channel.BasicConsume(queue: FromWebCompBot, autoAck: false, consumer: consumer);

            // Ожидание отмены задачи
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }

        private class Message
        {
            public string Id { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string AnswerContent { get; set; } = string.Empty;
        }
    }

}