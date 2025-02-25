using Fesenko_TBot.Interfaces;
using System.Globalization;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Serilog;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fesenko_TBot
{
    public class CallbackQueryHandler
    {
        private readonly ITelegramService _telegramService;
        private readonly IDatabaseService _databaseService;
        private int IdEng;
        private int IdInc;
        private string cityName;

        public CallbackQueryHandler(ITelegramService telegramService, IDatabaseService databaseService)
        {
            _telegramService = telegramService;
            _databaseService = databaseService;
        }

        public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;
            Log.Logger.Information("{MessageText} {ChatId}", data, chatId);
            if (data.StartsWith("city_", StringComparison.Ordinal))
            {
                cityName = data.Substring(5);
                await ShowIncidentsForCity(chatId, cityName);
            }
            else if (data.StartsWith("incident_", StringComparison.Ordinal))
            {
                IdInc = int.Parse(data.Substring(9));
                await ShowEngineersForIncident(chatId, IdInc);
            }
            else if (data.StartsWith("engineer_", StringComparison.Ordinal))
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

        private async Task ShowIncidentsForCity(long chatId, string cityName)
        {
            var incidents = await _databaseService.GetIncidentsByCityAsync(cityName);
            var messageBuilder = new StringBuilder($"Список открытых заявок в городе {cityName}\n\n");

            foreach (var incident in incidents)
            {
                var timeDifference = (incident.Deadline - DateTime.Now).TotalHours;
                if (timeDifference < 6)
                {
                    messageBuilder.AppendLine($"🔥<b>{incident.IdInc}: {incident.Description} (до {incident.Deadline})</b>");
                }
                else
                {
                    messageBuilder.AppendLine($"{incident.IdInc}: {incident.Description} (до {incident.Deadline})");
                }
            }

            var keyboard = new InlineKeyboardMarkup(incidents.Select(incident =>
                new[] {
            InlineKeyboardButton.WithCallbackData(
                $"{incident.IdInc}: {incident.Description}",
                $"incident_{incident.IdInc}")
                }).ToArray()
                .Concat(new[]
                {
            new[] { InlineKeyboardButton.WithCallbackData("🏘️  Вернуться к выбору города", "back_to_city") }
                }).ToArray()
            );

            await _telegramService.SendMessage(chatId, messageBuilder.ToString(), ParseMode.Html, replyMarkup: keyboard);
        }

        private async Task ShowEngineersForIncident(long chatId, int incidentId)
        {
            var incident = await _databaseService.GetIncidentByIdAsync(incidentId);
            var engineers = await _databaseService.GetEngineersByCityAsync(incident.City);

            var message = $"<b>Заявка: {incidentId}. Описание неисправности: {incident.Description}\nКонтрольный срок: {incident.Deadline} \nУслуга по договору: {incident.Service} \n\n👨‍🏭 Свободные инженеры:</b>\n";

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

            keyboardButtons.Add(new List<InlineKeyboardButton>
        {
        InlineKeyboardButton.WithCallbackData("📋  Вернуться к выбору заявок", "back_to_incidents"),
        InlineKeyboardButton.WithCallbackData("🏘️  Вернуться к выбору города", "back_to_city")
        });

            var keyboard = new InlineKeyboardMarkup(keyboardButtons);

            await _telegramService.SendMessage(chatId, message, ParseMode.Html, replyMarkup: keyboard);
        }

        private async Task ShowEngineerOptions(long chatId, int engineerId)
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

        private async Task ShowEngineerLocation(long chatId, int engineerId)
        {
            var engineer = await _databaseService.GetEngineerByIdAsync(engineerId);
            if (engineer != null)
            {

                if (string.IsNullOrWhiteSpace(engineer?.Coordinates) || !engineer.Coordinates.Contains(","))
                {
                    await _telegramService.SendMessage(chatId, "Ошибка: у инженера отсутствуют координаты.");
                    Log.Logger.Information("Ошибка: у инженера отсутствуют координаты.");
                    return;
                }
                var parts = engineer.Coordinates.Split(',');
                if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
                {
                    await _telegramService.SendLocation(chatId, latitude, longitude);
                }
                else
                {
                    await _telegramService.SendMessage(chatId, "Ошибка: неверный формат координат.");
                    Log.Logger.Information("Ошибка: неверный формат координат.");
                }

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🛠️  Назначить инженера", "assign_engineer") },
                new[] { InlineKeyboardButton.WithCallbackData("👨‍🏭  Вернуться к выбору инженеров", "back_to_engineers") },
                new[] { InlineKeyboardButton.WithCallbackData("📋  Вернуться к выбору заявок", "back_to_incidents") }
            });

                await _telegramService.SendMessage(chatId, "Местоположение инженера:", replyMarkup: keyboard);
            }
        }

        private async Task AssignEngineerToIncident(long chatId, int engineerId, int incidentId)
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

        private async Task ShowCitySelection(long chatId)
        {
            var cities = await _databaseService.GetCitiesAsync();
            var chunkedCities = cities.Select((city, index) => new { city, index })
                                      .GroupBy(x => x.index / 2)
                                      .Select(group => group.Select(x => InlineKeyboardButton.WithCallbackData(x.city, $"city_{x.city}")).ToArray())
                                      .ToArray();

            var keyboard = new InlineKeyboardMarkup(chunkedCities);

            await _telegramService.SendMessage(chatId, "Выберите город:", replyMarkup: keyboard);
        }
    }
}
