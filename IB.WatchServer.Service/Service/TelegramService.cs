﻿using IB.WatchServer.Service.Entity;
using IB.WatchServer.Service.Infrastructure;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
                var telegramUser = new TelegramUserInfo(message);

                if (message.Document != null)
                {
                    await ProcessMessage(telegramUser, message, ProcessGpx);
                }
                else if (message.Text != null)
                {
                    switch (message.Text.ToLower())
                    {
                        case "/start":
                            await ProcessMessage(telegramUser, message, MessageStart);
                            break;

                        case "/myid":
                            await ProcessMessage(telegramUser, message, MessageMyid);
                            break;

                        case "/list":
                            await ProcessMessage(telegramUser, message, MessageList);
                            break;

                        case var routeStr when new Regex("/renamelast ([^;]+)", RegexOptions.IgnoreCase).IsMatch(routeStr):
                            await ProcessMessage(telegramUser, message, MessageRenameLast);
                            break;

                        case var routeStr when new Regex("/rename:([0-9]+) ([^;]+)", RegexOptions.IgnoreCase).IsMatch(routeStr):
                            await ProcessMessage(telegramUser, message, MessageRename);
                            break;

                        case var routeStr when new Regex("/delete:([0-9]+)", RegexOptions.IgnoreCase).IsMatch(routeStr):
                            await ProcessMessage(telegramUser, message, MessageDelete);
                            break;

                        default:
                            await _telegramBot.SendTextMessageAsync(message.Chat, "Unknown command");
                            _logger.LogInformation("Unprocessed. From {@TelegramUser} has been received {Message}", telegramUser, message.Text);
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Error processing {@Message}, {@User}, {@Document}", message.Text, message.From, message.Document);
                await _telegramBot.SendTextMessageAsync(message.Chat, "Error, unable to process");
            }
        }

        private async Task<string> ProcessGpx(Message message, YasUser yasUser)
        {
            var fileId = message.Document.FileId;
            var fileName = message.Document.FileName;

            await using var memoryStream = new MemoryStream();  
            await _telegramBot.GetInfoAndDownloadFileAsync(fileId, memoryStream);
            memoryStream.Position = 0;
            XNamespace ns = "http://www.topografix.com/GPX/1/1"; 
            var root = XElement.Load(memoryStream);
            var orderId = 0;
            var points = root.Elements(ns + "rte").Elements(ns + "rtept")
                .Union(root.Elements(ns + "wpt"))
                .Select(w => new YasWaypoint()
                { 
                    Name = w.Element(ns + "name")?.Value,
                    Latitude = Convert.ToDecimal(w.Attribute("lat")?.Value),
                    Longitude = Convert.ToDecimal(w.Attribute("lon")?.Value),
                    OrderId = orderId++
                }).ToList();
            
            if (points.Count == 0)
                return $"No route or way points were found in {fileName} ";

            var route = new YasRoute
            {
                UserId = yasUser.UserId,
                UploadTime = DateTime.UtcNow,
                RouteName = root.Element(ns + "rte")?.Element(ns + "name")?.Value ?? Path.GetFileNameWithoutExtension(fileName) 
            };

            await using var db = _dbFactory.Create();
            await db.BeginTransactionAsync();
            route.RouteId = await db.GetTable<YasRoute>().DataContext.InsertWithInt64IdentityAsync(route);
            foreach(var point in points)
                point.RouteId = route.RouteId;
            db.BulkCopy(points);
            await db.CommitTransactionAsync();

            return $"The route * {route.RouteId} * : {route.RouteName} ({points.Count} way points) has been uploaded \n userId:{yasUser.PublicId}";
        }

        private async Task<string> MessageDelete(Message message, YasUser yasUser)
        {
            var groups = new Regex("/delete:([0-9]+)", RegexOptions.IgnoreCase).Match(message.Text).Groups;
            var routeId = Convert.ToInt64(groups[1].Value);

            await using var db = _dbFactory.Create();

            int count = await db.GetTable<YasRoute>()
                .Where(r => r.UserId == yasUser.UserId && r.RouteId == routeId)
                .DeleteAsync();

            return (count > 0)
                ? $"Route id: * {routeId} * has been deleted"
                : $"Cannot find the route id: * {routeId} *";
        }

        private async Task<string> MessageRenameLast(Message message, YasUser yasUser)
        {
            var groups = new Regex("/renamelast ([^;]+)", RegexOptions.IgnoreCase).Match(message.Text).Groups;
            var newName = groups[1].Value;
            
            await using var db = _dbFactory.Create();
                
            var routeId = await db.GetTable<YasRoute>().Where(r=>r.UserId == yasUser.UserId).MaxAsync(r=>r.RouteId);
            int count = await db.GetTable<YasRoute>()
                .Where(r => r.UserId == yasUser.UserId && r.RouteId == routeId)
                .Set(r => r.RouteName, newName)
                .UpdateAsync();

            return (count > 0)
                ? $"Route id: * {routeId} *, new name: * {newName} *"
                : $"Cannot find the route id: * {routeId} *";
        }

        private async Task<string> MessageRename(Message message, YasUser yasUser)
        {
            var groups = new Regex("/rename:([0-9]+) ([^;]+)", RegexOptions.IgnoreCase).Match(message.Text).Groups;
            var routeId = Convert.ToInt64(groups[1].Value);
            var newName = groups[2].Value;
            
            await using var db = _dbFactory.Create();
            
            int count = await db.GetTable<YasRoute>()
                .Where(r => r.UserId == yasUser.UserId && r.RouteId == routeId)
                .Set(r => r.RouteName, newName)
                .UpdateAsync();

            return (count > 0)
                ? $"Route id: * {routeId} *, new name: * {newName} *"
                : $"Cannot find the route id: * {routeId} *";
        }

        private async Task<string> MessageList(Message message, YasUser yasUser)
        {
            await using var db = _dbFactory.Create();
            var routes = db.GetTable<YasRoute>()
                .Where(u => u.UserId == yasUser.UserId).OrderByDescending(r=>r.RouteId).ToArray();

            return (routes.Length > 0)
                ? routes.Aggregate("", (o, r) => o + $"* {r.RouteId} * : ` {r.RouteName} \n({r.UploadTime})`\n\n")
                : "No routes found";
        }

        /// <summary>
        /// process /myId request
        /// </summary>
        /// <param name="message">telegram message</param>
        /// <param name="yasUser">User data</param>
        private async Task<string> MessageMyid(Message message, YasUser yasUser)
        {
            return await Task.FromResult(yasUser.PublicId);
        }

        private async Task<string> MessageStart(Message message, YasUser yasUser)
        {
            var output = "/myid `- return ID-string to identify your routes`\n\n" +
                "/list `- route list `\n\n" + "" +
                "/renamelast <new name> `- rename last uploaded route`\n\n " + 
                "/rename:<id> <new name> `- set <new name> to route with <id>`\n\n" + 
                "/delete:<id> `delete route with <id>`";
            return await Task.FromResult(output);
        }

        private async Task ProcessMessage(TelegramUserInfo telegramUser, Message message, Func<Message, YasUser, Task<string>> processAction)
        {
            var yasUser = await LoadOrCreateYasUser(telegramUser);

            var output = await processAction(message, yasUser);

            await _telegramBot.SendTextMessageAsync(message.Chat, output, ParseMode.Markdown);
            _logger.LogInformation("For {@TelegramUser} has been processed {Message} and returned {Output}", yasUser, message.Text, output);
        }

        private async Task<YasUser> LoadOrCreateYasUser(TelegramUserInfo telegramUser)
        {
            await using var db = _dbFactory.Create();
          
            var yasUserInfo = await db.GetTable<YasUser>()
                .Where(u => u.TelegramId == telegramUser.UserId) 
                .SingleOrDefaultAsync();

            if (yasUserInfo == null)
            { 
                yasUserInfo = new YasUser()
                {
                    TelegramId = telegramUser.UserId,
                    PublicId = shortid.ShortId.Generate(true, false, 10),
                    UserName = telegramUser.UserName,
                    RegisterTime = DateTime.UtcNow
                };
                yasUserInfo.UserId = await db.GetTable<YasUser>().DataContext.InsertWithInt64IdentityAsync(yasUserInfo);
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

        public long UserId { get; set; }

        public string UserName { get; set; }
    }

}
