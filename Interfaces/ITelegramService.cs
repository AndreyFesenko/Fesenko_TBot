using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Fesenko_TBot.Interfaces
{
    public interface ITelegramService
    {
        Task SendMessage(long chatId, string message, ParseMode parseMode = ParseMode.Html, IReplyMarkup replyMarkup = null);
        Task SendLocation(long chatId, double latitude, double longitude);
        Task StartReceiving(Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler, Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler);
    }
}
