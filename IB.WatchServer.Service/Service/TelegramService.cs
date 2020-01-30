//using System.Linq;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure.Linq2DB;
using LinqToDB.Common;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using System.Linq;
using System;

namespace IB.WatchServer.Service.Service
{
    public class TelegramService
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly DataConnectionFactory _dbFactory;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(ILogger<TelegramService> logger, ITelegramBotClient telegramBot, DataConnectionFactory dbFactory)
        {
            _telegramBot = telegramBot;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Telegram bot message handler
        /// </summary>
        public async void OnBotMessage(Message message) 
        {
            if (message.Text != null)
            {
                var telegramUser = new TelegramUserInfo(message);
                
                switch (message.Text.ToLower())
                {
                    case "/myid":
                        await ProcessMyid(message, telegramUser);
                        break;

                    case "/list":
                        await ProcessList(message, telegramUser);
                        break;

                    default:
                        await _telegramBot.SendTextMessageAsync(message.Chat, "Unknown commad");
                        _logger.LogInformation("Unprocessed. From {@TelegramUser} has been received {Message}", telegramUser, message.Text);
                        break;
                }
            }
        }

        private async Task ProcessList(Message message, TelegramUserInfo telegramUser)
        {
            string output = "";

            var yasUser = await LoadOrCreateYasUser(telegramUser);
            await using var db = _dbFactory.Create();
            { 
                var routes = db.GetTable<YasRouteInfo>().Where(u => u.UserId == yasUser.UserId);

                if (routes == null || routes.Count() == 0)
                {
                    output = "No routes available";
                }
                else
                {
                    foreach(var route in routes)
                    {
                        output += $"* {route.RouteId} * : ` {route.RouteName} \n({route.UploadTime})`\n\n";
                    }
                }
            }

            await _telegramBot.SendTextMessageAsync(message.Chat, output);

            _logger.LogInformation("For {@TelegramUser} has been processed {Message} and returned {Output}", yasUser, message.Text, output);
        }

        /// <summary>
        /// process /myId request
        /// </summary>
        /// <param name="message">telegram message</param>
        /// <param name="userInfo">User data</param>
        private async Task ProcessMyid(Message message, TelegramUserInfo telegramUser)
        {
            
            var yasUser = await LoadOrCreateYasUser(telegramUser);
            var output = yasUser.PublicId;
            await _telegramBot.SendTextMessageAsync(message.Chat, output);

            _logger.LogInformation("For {@TelegramUser} has been processed {Message} and returned {Output}", yasUser, message.Text, output);
        }

        private async Task<YasUserInfo> LoadOrCreateYasUser(TelegramUserInfo telegramUser)
        {
            await using var db = _dbFactory.Create();
          
            var yasUserInfo = await db.GetTable<YasUserInfo>()
                .Where(u => u.TelegramId == telegramUser.UserId) 
                .SingleOrDefaultAsync();

            if (yasUserInfo == null)
            { 
                yasUserInfo = new YasUserInfo()
                {
                    TelegramId = telegramUser.UserId,
                    PublicId = shortid.ShortId.Generate(true, false, 10),
                    UserName = telegramUser.UserName,
                    RegisterTime = DateTime.UtcNow
                };
                await db.GetTable<YasUserInfo>().DataContext.InsertAsync(yasUserInfo);
            }
            return yasUserInfo;
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
