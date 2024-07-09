using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreProcessor.RabbitMq
{
    public interface IRabbitMqService : IHostedService, IDisposable
    {
        // Метод для отправки сообщения в очередь
        void SendMessageToQueue(string queueName, string message);
        // Метод для подтверждения обработки сообщения
        Task AcknowledgeMessage(ulong deliveryTag);
        // Асинхронный метод для отклонения сообщения
        Task RejectMessage(ulong deliveryTag, bool requeue);
    }

}
