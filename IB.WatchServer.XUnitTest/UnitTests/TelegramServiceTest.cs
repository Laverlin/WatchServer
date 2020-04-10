using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Migrations;
using IB.WatchServer.Service.Service;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace IB.WatchServer.XUnitTest.UnitTests
{
    public class TelegramServiceTest : IDisposable
    {
        private IConnectionSettings _connectionSettings;
        private MigrationRunner _migrationRunner;

        public TelegramServiceTest()
        {
            // Prepare Database
            //
            _connectionSettings = TestHelper.GetConnectionSettings();
            _migrationRunner = new MigrationRunner(_connectionSettings.BuildConnectionString());
            _migrationRunner.RunMigrationsUp();
        }

        public void Dispose()
        {
            _migrationRunner.RunMigrationDown(new BaselineMigration());
        }

        [Fact]
        public void UnknownCommandShouldReturnUnknown()
        {
            // Arrange
            //
            var telegramBotMock = new Mock<ITelegramBotClient>();
            telegramBotMock.Setup(_ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), 
                It.IsAny<string>(), 
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()))
                .Verifiable();

            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                new DataConnectionFactory(_connectionSettings));

            var fakeMessage = new Message
            {
                Text = "fake message",
                Chat = new Chat
                {
                    FirstName = "1",
                    LastName = "2",
                    Id = 0
                },
                From = new User
                {
                    FirstName = "1",
                    LastName = "2",
                    Id = 0
                }
            };

            // Act
            //
            telegramService.OnBotMessage(fakeMessage);


            // Assert
            //
            telegramBotMock.Verify(_ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.Is<string>(m => m.Equals("Unknown command")),
                ParseMode.Default, false, false, 0, null, default));
        }

        [Fact]
        public async Task StartMessageShouldReturnStart()
        {
            // Arrange
            //
            var telegramBotMock = new Mock<ITelegramBotClient>();
            telegramBotMock.Setup(_ => _.SendTextMessageAsync(
                    It.IsAny<ChatId>(), 
                    It.IsAny<string>(), 
                    It.IsAny<ParseMode>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<int>(), 
                    It.IsAny<IReplyMarkup>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync<ITelegramBotClient, Message>(new Message())
                .Verifiable();

            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                new DataConnectionFactory(_connectionSettings));

            var fakeMessage = new Message
            {
                Text = "/start",
                Chat = new Chat
                {
                    FirstName = "1",
                    LastName = "2",
                    Id = 0
                },
                From = new User
                {
                    FirstName = "1",
                    LastName = "2",
                    Id = 0
                }
            };

            // Act
            //
            await telegramService.OnBotMessage(fakeMessage);


            // Assert
            //
            telegramBotMock.Verify( _ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.Is<string>(m => m.StartsWith("/myid <code>- returns")),
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()));
        }


    }
}
