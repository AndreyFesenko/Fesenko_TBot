using Fesenko_TBot.Interfaces;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Fesenko_TBot.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _botClient;

        public TelegramService(string botToken)
        {
            _botClient = new TelegramBotClient(botToken);
        }

        public async Task SendMessage(long chatId, string message, ParseMode parseMode = ParseMode.Html, IReplyMarkup replyMarkup = null)
        {
            await _botClient.SendMessage(chatId, message, parseMode, replyMarkup: replyMarkup);
        }

        public async Task SendLocation(long chatId, double latitude, double longitude)
        {
            await _botClient.SendLocation(chatId, latitude, longitude);
        }

        public async Task StartReceiving(Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler, Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler)
        {
            _botClient.StartReceiving(updateHandler, errorHandler);
        }
    }
}
