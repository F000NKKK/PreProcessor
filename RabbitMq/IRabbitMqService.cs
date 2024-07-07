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
        void SendMessageToQueue(string queueName, string message);
        void AcknowledgeMessage(ulong deliveryTag);
        void RejectMessage(ulong deliveryTag, bool requeue);
    }

}
