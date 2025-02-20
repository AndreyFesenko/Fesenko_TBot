using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;
using Serilog;
using Serilog.Formatting.Compact;
using Fesenko_TBot.Interfaces;
using Fesenko_TBot.Services;
using Fesenko_TBot.Models;

namespace Fesenko_TBot
{

    public enum UserState
    {
        None,       // Нет активного состояния
        AwaitingLogin, // Ожидание ввода логина
        AwaitingPassword // Ожидание ввода пароля
    }
    class Program
    {

        private static Dictionary<long, UserState> _userStates = new Dictionary<long, UserState>();
        private static Dictionary<long, string> _userLogins = new Dictionary<long, string>(); // Для временного хранения логина

        private static ITelegramService _telegramService;
        private static IDatabaseService _databaseService;
        private static AuthService _authService;

        private static int IdEng;
        private static int IdInc;
        private static string cityName;

        static async Task Main(string[] args)
        {
            //Подключаем логгер
            var config = new Config();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .WriteTo.File(new RenderedCompactJsonFormatter(), config.LogFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var dbContext = new OctopusDbContext();
            _databaseService = new DatabaseService(dbContext);
            _telegramService = new TelegramService(Environment.GetEnvironmentVariable("BotToken", EnvironmentVariableTarget.User));

            _authService = new AuthService(_databaseService);

            _telegramService.StartReceiving(UpdateHandler, ErrorHandler);

            Log.Logger.Information("Бот запущен. Нажмите Enter для выхода!");
            Console.ReadLine();
        }

        private static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;
                Log.Logger.Information("{MessageText} {ChatId}", messageText, chatId);

                // Проверяем текущее состояние пользователя
                if (!_userStates.ContainsKey(chatId))
                {
                    _userStates[chatId] = UserState.None;
                }

                switch (_userStates[chatId])
                {
                    case UserState.None:
                        if (messageText == "/start")
                        {
                            await _telegramService.SendMessage(chatId, "Введите ваш логин:");
                            _userStates[chatId] = UserState.AwaitingLogin; // Переводим в состояние ожидания логина
                        }
                        break;

                    case UserState.AwaitingLogin:
                        // Сохраняем логин и запрашиваем пароль
                        _userLogins[chatId] = messageText;
                        await _telegramService.SendMessage(chatId, "Введите ваш пароль:");
                        _userStates[chatId] = UserState.AwaitingPassword; // Переводим в состояние ожидания пароля
                        break;

                    case UserState.AwaitingPassword:
                        // Получаем логин из временного хранилища
                        var login = _userLogins[chatId];
                        var password = messageText;

                        // Проверяем авторизацию
                        var isAuthenticated = await _authService.AuthenticateUserAsync(login, password);

                        if (isAuthenticated)
                        {
                            await _telegramService.SendMessage(chatId, "Авторизация успешна!");
                            await ShowCitySelection(chatId);
                        }
                        else
                        {
                            await _telegramService.SendMessage(chatId, "Неверный логин или пароль. Чтобы повторно ввести пароль нажмите /start");
                        }

                        // Сбрасываем состояние пользователя
                        _userStates[chatId] = UserState.None;
                        _userLogins.Remove(chatId); // Удаляем временный логин
                        break;
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackQuery = update.CallbackQuery;
                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;
                Log.Logger.Information("{MessageText} {ChatId}", callbackQuery.Data, chatId);
             
                if (data.StartsWith("city_"))
                {
                    cityName = data.Substring(5);
                    await ShowIncidentsForCity(chatId, cityName);
                }
                else if (data.StartsWith("incident_"))
                {
                    IdInc = int.Parse(data.Substring(9));
                    await ShowEngineersForIncident(chatId, IdInc);
                }
                else if (data.StartsWith("engineer_"))
                {
                    IdEng = int.Parse(data.Substring(9));
                    await ShowEngineerOptions(chatId, IdEng);
                }
                else if (data == "show_location")
                {
                    await ShowEngineerLocation(chatId, IdEng);
                }
                else if (data == "assign_engineer")
                {
                    await AssignEngineerToIncident(chatId, IdEng, IdInc);
                }
                else if (data == "back_to_city")
                {
                    await ShowCitySelection(chatId);
                }
                else if (data == "back_to_incidents")
                {
                    await ShowIncidentsForCity(chatId, cityName);
                }
                else if (data == "back_to_engineers")
                {
                    await ShowEngineersForIncident(chatId, IdInc);
                }
            }
        }

        private static Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            Log.Logger.Error($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

        private static async Task ShowCitySelection(long chatId)
        {
            var cities = await _databaseService.GetCitiesAsync();
            var chunkedCities = cities.Select((city, index) => new { city, index })
                                      .GroupBy(x => x.index / 2) // Разделить на группы по 2 города
                                      .Select(group => group.Select(x => InlineKeyboardButton.WithCallbackData(x.city, $"city_{x.city}")).ToArray())
                                      .ToArray();

            var keyboard = new InlineKeyboardMarkup(chunkedCities);

            await _telegramService.SendMessage(chatId, "Выберите город:", replyMarkup: keyboard);
        }

        private static async Task ShowIncidentsForCity(long chatId, string cityName)
        {
            var incidents = await _databaseService.GetIncidentsByCityAsync(cityName);
            var message = $"Список открытых заявок в городе {cityName}\n\n";

            // Строки с заявками для отправки как обычное сообщение, не как кнопки
            foreach (var incident in incidents)
            {
                var timeDifference = (incident.Deadline - DateTime.Now).TotalHours;
                if (timeDifference < 6)
                {
                    message += $"🔥<b>{incident.IdInc}: {incident.Description} (до {incident.Deadline})</b>\n";
                }
                else
                {
                    message += $"{incident.IdInc}: {incident.Description} (до {incident.Deadline})\n";
                }
            }

            // Формируем inline-кнопки, каждая кнопка будет в своей строке
            var keyboard = new InlineKeyboardMarkup(incidents.Select(incident =>
                new[] {
            InlineKeyboardButton.WithCallbackData(
                $"{incident.IdInc}: {incident.Description}", // ID и описание заявки
                $"incident_{incident.IdInc}") // CallbackData для обработки
                }).ToArray()
                .Concat(new[]
                {
            new[] { InlineKeyboardButton.WithCallbackData("🏘️  Вернуться к выбору города", "back_to_city") }
                }).ToArray()
            );

            // Отправляем сообщение с кнопками
            await _telegramService.SendMessage(chatId, message, ParseMode.Html, replyMarkup: keyboard);
        }


        private static async Task ShowEngineersForIncident(long chatId, int incidentId)
        {
            var incident = await _databaseService.GetIncidentByIdAsync(incidentId);
            var engineers = await _databaseService.GetEngineersByCityAsync(incident.City);

            var message = $"<b>Заявка: {incidentId}. Описание неисправности: {incident.Description}\nКонтрольный срок: {incident.Deadline} \nУслуга по договору: {incident.Service} \n\n👨‍🏭 Свободные инженеры:</b>\n";


            // Формируем клавиатуру с двумя инженерами в строке
            var keyboardButtons = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < engineers.Count; i += 2)
            {
                var row = new List<InlineKeyboardButton>();
                row.Add(InlineKeyboardButton.WithCallbackData(engineers[i].NameEng, $"engineer_{engineers[i].IdEng}"));

                if (i + 1 < engineers.Count)
                {
                    row.Add(InlineKeyboardButton.WithCallbackData(engineers[i + 1].NameEng, $"engineer_{engineers[i + 1].IdEng}"));
                }

                keyboardButtons.Add(row);
            }

            // Добавляем кнопки для возвращения
            keyboardButtons.Add(new List<InlineKeyboardButton>
        {
        InlineKeyboardButton.WithCallbackData("📋  Вернуться к выбору заявок", "back_to_incidents"),
        InlineKeyboardButton.WithCallbackData("🏘️  Вернуться к выбору города", "back_to_city")
        });

            var keyboard = new InlineKeyboardMarkup(keyboardButtons);

            await _telegramService.SendMessage(chatId, message, ParseMode.Html, replyMarkup: keyboard);
        }

        private static async Task ShowEngineerOptions(long chatId, int engineerId)
        {
            var engineer = await _databaseService.GetEngineerByIdAsync(engineerId);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[] { InlineKeyboardButton.WithCallbackData("🗺️  Вывести местоположение инженера", "show_location") },
            new[] { InlineKeyboardButton.WithCallbackData("🛠️  Назначить инженера", "assign_engineer") },
            new[] { InlineKeyboardButton.WithCallbackData("👨‍🏭  Вернуться к выбору инженеров", "back_to_engineers") },
            new[] { InlineKeyboardButton.WithCallbackData("📋  Вернуться к выбору заявок", "back_to_incidents") },
            new[] { InlineKeyboardButton.WithCallbackData("🏘️  Вернуться к выбору города", "back_to_city")}
        });

            await _telegramService.SendMessage(chatId, $"Выбран инженер: {engineer.NameEng} \nВыберите действие:", replyMarkup: keyboard);
        }

        private static async Task ShowEngineerLocation(long chatId, int engineerId)
        {
            var engineer = await _databaseService.GetEngineerByIdAsync(engineerId);
            if (engineer != null)
            {
                string[] parts = engineer.Coordinates.Split(',');
                double latitude = double.Parse(parts[0], CultureInfo.InvariantCulture);
                double longitude = double.Parse(parts[1], CultureInfo.InvariantCulture);
                await _telegramService.SendLocation(chatId, latitude, longitude);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🛠️  Назначить инженера", "assign_engineer") },
                new[] { InlineKeyboardButton.WithCallbackData("👨‍🏭  Вернуться к выбору инженеров", "back_to_engineers") },
                new[] { InlineKeyboardButton.WithCallbackData("📋  Вернуться к выбору заявок", "back_to_incidents") }
            });

                await _telegramService.SendMessage(chatId, "Местоположение инженера:", replyMarkup: keyboard);
            }
        }

        private static async Task AssignEngineerToIncident(long chatId, int engineerId, int incidentId)
        {
            await _databaseService.AssignEngineerToIncidentAsync(engineerId, incidentId);
            var engineer = await _databaseService.GetEngineerByIdAsync(engineerId);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
            new[] { InlineKeyboardButton.WithCallbackData("📋  Вернуться к выбору заявок", "back_to_incidents") },
            new[] { InlineKeyboardButton.WithCallbackData("🏘️  Вернуться к выбору города", "back_to_city")}
            });

            await _telegramService.SendMessage(chatId, $"Ура😀. Инженер {engineer.NameEng} назначен на заявку {incidentId}.", replyMarkup: keyboard);
        }
    }
}

