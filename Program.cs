using Telegram.Bot.Types.Enums;
using Serilog;
using Serilog.Formatting.Compact;
using Fesenko_TBot.Interfaces;
using Fesenko_TBot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace Fesenko_TBot
{

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new Config();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .WriteTo.File(new RenderedCompactJsonFormatter(), config.LogFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            //Используем Generic Host для управления жизненным циклом приложения и регистрируем зависимости в DI-контейнере
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<OctopusDbContext>();
                    services.AddSingleton<Config>();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    var botToken = Environment.GetEnvironmentVariable("BotToken", EnvironmentVariableTarget.User);
                    if (string.IsNullOrWhiteSpace(botToken))
                    {
                        throw new InvalidOperationException("BotToken не задан в переменных окружения.");
                    }
                    services.AddSingleton<ITelegramService>(provider => new TelegramService(botToken));
                    services.AddSingleton<AuthService>();
                    services.AddSingleton<MessageHandler>();
                    services.AddSingleton<CallbackQueryHandler>();
                })
                .Build();

            //Получение сервисов из DI-контейнера
            var telegramService = host.Services.GetRequiredService<ITelegramService>();
            var messageHandler = host.Services.GetRequiredService<MessageHandler>();
            var callbackQueryHandler = host.Services.GetRequiredService<CallbackQueryHandler>();

            //Запуск обработки входящих сообщений
            _ = telegramService.StartReceiving(async (bot, update, cancellationToken) =>
            {
                if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
                {
                    await messageHandler.HandleMessageAsync(update.Message);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    await callbackQueryHandler.HandleCallbackQueryAsync(update.CallbackQuery);
                }
            }, (bot, exception, cancellationToken) =>
            {
                Log.Logger.Error($"Ошибка: {exception.Message}");
                return Task.CompletedTask;
            });

            Log.Logger.Information("Бот запущен. Нажмите Enter для выхода!");
            Console.ReadLine();
        }
    }
}


