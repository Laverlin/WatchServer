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
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;

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
            try
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

                        case var routeStr when new Regex("/renamelast ([^;]+)", RegexOptions.IgnoreCase).IsMatch(routeStr):
                            await ProcessRenameLast(message, telegramUser);
                            break;

                        case var routeStr when new Regex("/rename:([0-9]+) ([^;]+)", RegexOptions.IgnoreCase).IsMatch(routeStr):
                            await ProcessRename(message, telegramUser);
                            break;

                        default:
                            await _telegramBot.SendTextMessageAsync(message.Chat, "Unknown commad");
                            _logger.LogInformation("Unprocessed. From {@TelegramUser} has been received {Message}", telegramUser, message.Text);
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Error processsing {@Message}", message);
                await _telegramBot.SendTextMessageAsync(message.Chat, "Error, unable to process");
            }
        }

        private async Task ProcessRenameLast(Message message, TelegramUserInfo telegramUser)
        {
            var yasUser = await LoadOrCreateYasUser(telegramUser);
            var groups = new Regex("/renamelast ([^;]+)", RegexOptions.IgnoreCase).Match(message.Text).Groups;
            var newName = groups[1].Value;
            
            string output = "";
            await using var db = _dbFactory.Create();
            {
                var routeId = await db.GetTable<YasRouteInfo>().Where(r=>r.UserId == yasUser.UserId).MaxAsync(r=>r.RouteId);
                int count = await db.GetTable<YasRouteInfo>()
                    .Where(r => r.UserId == yasUser.UserId && r.RouteId == routeId)
                    .Set(r => r.RouteName, newName)
                    .UpdateAsync();

                output = (count > 0)
                    ? $"Route id: * {routeId} *, new name: * {newName} *"
                    : $"cannot find route id: * {routeId} *";
            }

            await _telegramBot.SendTextMessageAsync(message.Chat, output.Replace("-", @"\-"), ParseMode.MarkdownV2);
            _logger.LogInformation("For {@TelegramUser} has been processed {Message} and returned {Output}", yasUser, message.Text, output);
        }

        private async Task ProcessRename(Message message, TelegramUserInfo telegramUser)
        {
            var yasUser = await LoadOrCreateYasUser(telegramUser);
            var groups = new Regex("/rename:([0-9]+) ([^;]+)", RegexOptions.IgnoreCase).Match(message.Text).Groups;
            var routeId = Convert.ToInt64(groups[1].Value);
            var newName = groups[2].Value;
            
            string output = "";
            await using var db = _dbFactory.Create();
            {
                int count = await db.GetTable<YasRouteInfo>()
                    .Where(r => r.UserId == yasUser.UserId && r.RouteId == routeId)
                    .Set(r => r.RouteName, newName)
                    .UpdateAsync();

                output = (count > 0)
                    ? $"Route id: * {routeId} *, new name: * {newName} *"
                    : $"cannot find route id: * {routeId} *";
            }

            await _telegramBot.SendTextMessageAsync(message.Chat, output, ParseMode.MarkdownV2);
            _logger.LogInformation("For {@TelegramUser} has been processed {Message} and returned {Output}", yasUser, message.Text, output);
        }

        private async Task ProcessList(Message message, TelegramUserInfo telegramUser)
        {
            string output = "No routes available";

            var yasUser = await LoadOrCreateYasUser(telegramUser);
            await using var db = _dbFactory.Create();
            { 
                var routes = db.GetTable<YasRouteInfo>()
                    .Where(u => u.UserId == yasUser.UserId).OrderByDescending(r=>r.RouteId).ToArray();
                if (routes != null && routes.Length > 0)
                    output = routes.Aggregate("", (o, r) => o + $"* {r.RouteId} * : ` {r.RouteName} \n({r.UploadTime})`\n\n");
            }

            await _telegramBot.SendTextMessageAsync(message.Chat, output, ParseMode.MarkdownV2);
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
