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

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Config>();
                    services.AddDbContext<OctopusDbContext>();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddSingleton<ITelegramService>(provider =>
                        new TelegramService(Environment.GetEnvironmentVariable("BotToken", EnvironmentVariableTarget.User)));
                    services.AddSingleton<AuthService>();
                    services.AddSingleton<MessageHandler>();
                    services.AddSingleton<CallbackQueryHandler>();
                })
                .Build();

            var telegramService = host.Services.GetRequiredService<ITelegramService>();
            var messageHandler = host.Services.GetRequiredService<MessageHandler>();
            var callbackQueryHandler = host.Services.GetRequiredService<CallbackQueryHandler>();

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


