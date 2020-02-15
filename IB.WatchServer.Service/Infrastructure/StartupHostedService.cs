using System.Threading;
using System.Threading.Tasks;
using IB.WatchServer.Service.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Helper class to put some info in log output
    /// </summary>
    public class StartupHostedService : IHostedService
    {
        private readonly ILogger<StartupHostedService> _logger;
        private readonly ITelegramBotClient _telegramClient;
        private readonly TelegramService _telegramService;

        public StartupHostedService(ILogger<StartupHostedService> logger, ITelegramBotClient telegramClient, TelegramService telegramService)
        {
            _logger = logger;
            _telegramClient = telegramClient;
            _telegramService = telegramService;
        }

        /// <summary>
        /// On start, up telegram service
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start: {app}, version: {version}",
                SolutionInfo.Name,
                SolutionInfo.Version);


            _telegramClient.OnMessage += (sender, e) => _telegramService.OnBotMessage(e.Message);
            _telegramClient.StartReceiving();

            var me = await _telegramClient.GetMeAsync();
            _logger.LogInformation("The bot {BotId} has been started, name is {BotName}", me.Id, me.FirstName);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _telegramClient.StopReceiving();

            await Task.CompletedTask;
        }
    }
}
