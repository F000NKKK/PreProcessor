using System.Threading;
using System.Threading.Tasks;

namespace PreProcessor.RabbitMq
{
    public interface IRabbitMqBackgroundService
    {
        Task ProcessMessagesAsync(CancellationToken cancellationToken);
    }
}
