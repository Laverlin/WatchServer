using System.Threading.Tasks;
using LinqToDB.Common;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace IB.WatchServer.Service.Service
{
    public class TelegramService
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(ILogger<TelegramService> logger, ITelegramBotClient telegramBot)
        {
            _telegramBot = telegramBot;
            _logger = logger;
        }

        /// <summary>
        /// Telegram bot message handler
        /// </summary>
        public async void OnBotMessage(Message message) 
        {
            if (message.Text != null)
            {
                var userInfo = new TelegramUserInfo(message);
                
                switch (message.Text.ToLower())
                {
                    case "/myid":
                        await processMyid(message, userInfo);
                        break;

                    default:
                        await _telegramBot.SendTextMessageAsync(message.Chat, "Unknown commad");
                        _logger.LogInformation("Unprocessed. From {@TelegramUser} has been received {Message}", userInfo, message.Text);
                        break;
                }
            }
        }

        /// <summary>
        /// process /myId request
        /// </summary>
        /// <param name="message">telegram message</param>
        /// <param name="userInfo">User data</param>
        private async Task processMyid(Message message, TelegramUserInfo userInfo)
        {
            var output = userInfo.UserId.ToString();
            await _telegramBot.SendTextMessageAsync(
                    chatId: message.Chat,
                    text:   userInfo.UserId.ToString());

            _logger.LogInformation("For {@TelegramUser} has been processed {Message} and returned {Output}", userInfo, message.Text, output);
        }
    }

    public class TelegramUserInfo
    {
        public TelegramUserInfo(Message telegramMessage)
        {
            var username = telegramMessage.From.Username.IsNullOrEmpty() ? "" : $"({telegramMessage.From.Username})";
            UserId = telegramMessage.From.Id;
            UserName = $"{telegramMessage.From.FirstName} {telegramMessage.From.LastName}  {username}".Trim();
        }
        public long UserId {get;set;}
        public string UserName {get;set;}
    }

}
