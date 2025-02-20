using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Fesenko_TBot.Interfaces
{
    public interface ITelegramService
    {
        Task SendMessage(long chatId, string message, ParseMode parseMode = ParseMode.Html, IReplyMarkup replyMarkup = null);
        Task SendLocation(long chatId, double latitude, double longitude);
        Task StartReceiving(Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler, Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler);
    }
}
