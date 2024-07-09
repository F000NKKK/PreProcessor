using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using PreProcessor.RabbitMq;

namespace PreProcessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Настройка сервиса Windows, если приложение будет работать как Windows Service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = ".NET RabbitMq Service";
            });

            // Регистрация RabbitMqService и фонового сервиса
            builder.Services.AddScoped<IRabbitMqService, PreProcessorBackgroundService>();
            builder.Services.AddScoped<IRabbitMqBackgroundService, PreProcessorBackgroundService>();
            builder.Services.AddHostedService<PreProcessorBackgroundService>();

            // Настройка NLog
            builder.Logging.ClearProviders();
            builder.Logging.AddNLog("nlog.config");

            var app = builder.Build();

            // Запуск приложения
            app.Run();

            // Завершение записи логов перед закрытием приложения
            LogManager.Shutdown();
        }
    }
}
