using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PreProcessor.RabbitMq;

namespace PreProcessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = ".NET RabbitMq Service";
            });

            // Register RabbitMqService and the background service
            builder.Services.AddScoped<IRabbitMqService, PreProcessorBackgroundService>();
            builder.Services.AddScoped<IRabbitMqBackgroundService, PreProcessorBackgroundService>();
            builder.Services.AddHostedService<PreProcessorBackgroundService>();

            var app = builder.Build();

            app.Run();
        }
    }
}

