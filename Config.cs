using Microsoft.Extensions.Configuration;

namespace Fesenko_TBot;
public class Config
{
    public string LogFilePath { get; }

    public Config()
    {
        // Загружаем конфигурацию из appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Получаем путь к директории логов из переменной окружения
        string? logDir = Environment.GetEnvironmentVariable("LOG_DIR", EnvironmentVariableTarget.User);

        // Если переменная не установлена, берем путь из конфигурации
        if (string.IsNullOrEmpty(logDir))
        {
            LogFilePath = configuration["Logging:LogFilePath"] ?? "logs/log.json";
        }
        else
        {
            LogFilePath = Path.Combine(logDir, "log.json");
        }

        // Создаем директорию, если ее нет
        string? logDirectory = Path.GetDirectoryName(LogFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }
}
