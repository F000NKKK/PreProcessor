using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NLog;

namespace PreProcessor.RabbitMq
{
    public class PreProcessorBackgroundService : BackgroundService, IRabbitMqService, IRabbitMqBackgroundService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
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

            // Установка ограничения на количество неподтверждённых сообщений
            _channel.BasicQos(0, 1, false);
        }

        public void SendMessageToQueue(string queueName, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
            Logger.Info($"Сообщение отправлено в {queueName}");
        }

        public async Task AcknowledgeMessage(ulong deliveryTag)
        {
            await Task.Run(() => _channel.BasicAck(deliveryTag, false));
        }

        public async Task RejectMessage(ulong deliveryTag, bool requeue)
        {
            await Task.Run(() => _channel.BasicReject(deliveryTag, requeue));
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

                var flag = (deserializedMessage.Content != null) ? "NotNull" : "Null";
                Logger.Info($"\nПолучено сообщение:\nId: {deserializedMessage.Id}\nContent: {flag}\nMessageCurrentTime: {deserializedMessage.MessageCurrentTime}");

                try
                {
                    // Обработка сообщения
                    Logger.Info($"Обработка сообщения с ID: {deserializedMessage.Id}");

                    deserializedMessage.Id += "#" + (Convert.ToInt32(deserializedMessage.Id.Split("$")[0]) + 1).ToString() + "$" + "Bot";
                    deserializedMessage.IsUserMessage = false;

                    SendMessageToQueue(ProcessorQueueName, JsonSerializer.Serialize(deserializedMessage));

                    // Подтверждение сообщения
                    await AcknowledgeMessage(ea.DeliveryTag);
                    Logger.Info($"Сообщение подтверждено с deliveryTag: {ea.DeliveryTag}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Произошла ошибка: {ex.Message}");
                    // Отклонение сообщения в случае ошибки
                    await RejectMessage(ea.DeliveryTag, true);
                }
            };

            _channel.BasicConsume(queue: FromWebCompBot, autoAck: false, consumer: consumer);

            // Ожидание отмены задачи
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при закрытии соединения: {ex.Message}");
            }
            finally
            {
                base.Dispose();
            }
        }

        // Класс для представления сообщения.
        public class Message
        {
            public string Id { get; set; } = string.Empty; // Идентификатор сообщения
            public string Content { get; set; } = string.Empty; // Содержимое сообщения
            public string MessageCurrentTime { get; set; } = string.Empty; // Время отправки сообщения
            public Boolean IsUserMessage { get; set; } = true; // Флаг User/Bot, True/False соответственно
        }
    }
}
