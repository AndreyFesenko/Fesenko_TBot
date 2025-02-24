using Fesenko_TBot.Interfaces;
using Fesenko_TBot.Services;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Serilog;

namespace Fesenko_TBot
{
    public enum UserState
    {
        None,       // Нет активного состояния
        AwaitingLogin, // Ожидание ввода логина
        AwaitingPassword // Ожидание ввода пароля
    }
    public class MessageHandler
    {

        private readonly ITelegramService _telegramService;
        private readonly AuthService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly Dictionary<long, UserState> _userStates = new Dictionary<long, UserState>();
        private readonly Dictionary<long, string> _userLogins = new Dictionary<long, string>();

        public MessageHandler(ITelegramService telegramService, AuthService authService, IDatabaseService databaseService)
        {
            _telegramService = telegramService;
            _authService = authService;
            _databaseService = databaseService;
        }

        public async Task HandleMessageAsync(Message message)
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text;

            Log.Logger.Information("{MessageText} {ChatId}", messageText, chatId);

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
                        _userStates[chatId] = UserState.AwaitingLogin;
                    }
                    break;

                case UserState.AwaitingLogin:
                    _userLogins[chatId] = messageText;
                    await _telegramService.SendMessage(chatId, "Введите ваш пароль:");
                    _userStates[chatId] = UserState.AwaitingPassword;
                    break;

                case UserState.AwaitingPassword:
                    var login = _userLogins[chatId];
                    var password = messageText;

                    var isAuthenticated = await _authService.AuthenticateUserAsync(login, password);

                    if (isAuthenticated)
                    {
                        Log.Logger.Information($"Авторизация пользователя {login} успешна.");
                        await _telegramService.SendMessage(chatId, "Авторизация успешна!");
                        await ShowCitySelection(chatId);
                    }
                    else
                    {
                        Log.Logger.Information($"Пользователь {login} не прошел авторизацию.");
                        await _telegramService.SendMessage(chatId, "Неверный логин или пароль. Чтобы повторно ввести пароль нажмите /start");
                    }

                    _userStates[chatId] = UserState.None;
                    _userLogins.Remove(chatId);
                    break;
            }
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
