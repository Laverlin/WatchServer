using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IB.WatchServer.Service.Entity.SailingApp;
using IB.WatchServer.Service.Entity.Settings;
using IB.WatchServer.Service.Infrastructure;
using IB.WatchServer.Service.Migrations;
using IB.WatchServer.Service.Service;
using LinqToDB;
using LinqToDB.Tools;
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

        private Message _mockMessage = new Message
        {
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
        public async Task NewUserShouldbeCreatedInDb()
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
                .ReturnsAsync(new Message())
                .Verifiable();

            var dcFactory = new DataConnectionFactory(_connectionSettings);
            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                dcFactory);

            _mockMessage.Text = "/myid";


            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


            // Assert
            //
            telegramBotMock.Verify( _ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.IsAny<string>(),
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()), Times.Once);

            long usersCount;
            await using var dc2 = dcFactory.Create();
            {
                usersCount = await dc2.GetTable<YasUser>().CountAsync();
            }
            Assert.Equal(1, usersCount);
        }

        [Fact]
        public async Task UnknownCommandShouldReturnUnknown()
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

            _mockMessage.Text = "fake message";

            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


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
                .ReturnsAsync(new Message())
                .Verifiable();

            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                new DataConnectionFactory(_connectionSettings));

            _mockMessage.Text = "/start";

            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


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


        [Fact]
        public async Task MyIdShouldReturnId()
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
                .ReturnsAsync(new Message())
                .Verifiable();

            var dcFactory = new DataConnectionFactory(_connectionSettings);
            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                dcFactory);

            _mockMessage.Text = "/myid";

            var publicUserId = shortid.ShortId.Generate();
            var yasUser = new YasUser {TelegramId = 0, PublicId = publicUserId, UserName = "test-user-name"};
            await using var dc = dcFactory.Create();
            {
                dc.GetTable<YasUser>().DataContext.InsertWithIdentity(yasUser);
            }

            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


            // Assert
            //
            telegramBotMock.Verify( _ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.Is<string>(m => m.Equals($"{publicUserId}")),
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()));

            long usersCount;
            await using var dc2 = dcFactory.Create();
            {
                usersCount = await dc2.GetTable<YasUser>().CountAsync();
            }
            Assert.Equal(1, usersCount);
        }

        [Fact]
        public async Task RouteListShouldReturnList()
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
                .ReturnsAsync(new Message())
                .Verifiable();

            var dcFactory = new DataConnectionFactory(_connectionSettings);
            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                dcFactory);

            _mockMessage.Text = "/list";

            var publicUserId = shortid.ShortId.Generate();
            var yasUser = new YasUser {TelegramId = 0, PublicId = publicUserId, UserName = "test-user-name"};
            var yasRoute = new YasRoute {RouteName = "route1"};
            await using var dc = dcFactory.Create();
            {
                var userId = await dc.GetTable<YasUser>().DataContext.InsertWithInt64IdentityAsync(yasUser);
                yasRoute.UserId = userId; 
                dc.GetTable<YasRoute>().DataContext.InsertWithIdentity(yasRoute);
            }

            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


            // Assert
            //
            telegramBotMock.Verify( _ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.Is<string>(m => m.StartsWith("<b> 1 </b> : <code>route1")),
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()));

        }
        
        
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task DeleteCommandShouldDeleteSpecifedRoute(long routeId)
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
                .ReturnsAsync(new Message())
                .Verifiable();

            var dcFactory = new DataConnectionFactory(_connectionSettings);
            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                dcFactory);


            var yasUser = new YasUser {TelegramId = 0, PublicId = shortid.ShortId.Generate(), UserName = "test-user-name"};
            var yasRoute1 = new YasRoute {RouteName = "route-1"};
            var yasRoute2 = new YasRoute {RouteName = "route-2"};

            await using var dc = dcFactory.Create();
            {
                var userId = await dc.GetTable<YasUser>().DataContext.InsertWithInt64IdentityAsync(yasUser);
                yasRoute1.UserId = userId; 
                dc.GetTable<YasRoute>().DataContext.InsertWithIdentity(yasRoute1);
                dc.GetTable<YasRoute>().DataContext.InsertWithIdentity(yasRoute2);
                _mockMessage.Text = $"/delete:{routeId}";
            }


            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


            // Assert
            //
            telegramBotMock.Verify( _ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.Is<string>(
                    m => (routeId == 1 && m.Equals($"Route id: <b> {routeId} </b> has been deleted"))
                    || (routeId != 1 && m.Equals($"Cannot find the route id: <b> {routeId} </b>"))),
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()));

            await using var dc2 = dcFactory.Create();
            {
                var route1Count = await dc2.GetTable<YasRoute>().Where(r =>r.RouteName=="route-1").CountAsync();
                var route2Count = await dc2.GetTable<YasRoute>().Where(r =>r.RouteName=="route-2").CountAsync();

                var expected = routeId == 1 ? 0 : 1;
                Assert.Equal(expected, route1Count);
                Assert.Equal(1, route2Count);
            }
        }


        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task RenameCommandShouldRenameSpecifedRoute(long routeId)
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
                .ReturnsAsync(new Message())
                .Verifiable();

            var dcFactory = new DataConnectionFactory(_connectionSettings);
            var telegramService = new TelegramService(
                TestHelper.GetLoggerMock<TelegramService>().Object,
                telegramBotMock.Object,
                dcFactory);


            var yasUser = new YasUser {TelegramId = 0, PublicId = shortid.ShortId.Generate(), UserName = "test-user-name"};
            var yasRoute1 = new YasRoute {RouteName = "route-1"};
            var yasRoute2 = new YasRoute {RouteName = "route-2"};

            await using var dc = dcFactory.Create();
            {
                var userId = await dc.GetTable<YasUser>().DataContext.InsertWithInt64IdentityAsync(yasUser);
                yasRoute1.UserId = userId; 
                dc.GetTable<YasRoute>().DataContext.InsertWithIdentity(yasRoute1);
                dc.GetTable<YasRoute>().DataContext.InsertWithIdentity(yasRoute2);
                _mockMessage.Text = $"/rename:{routeId} new-name";
            }


            // Act
            //
            await telegramService.OnBotMessage(_mockMessage);


            // Assert
            //
            telegramBotMock.Verify( _ => _.SendTextMessageAsync(
                It.IsAny<ChatId>(), It.Is<string>(
                    m => (routeId == 1 && m.Equals($"Route id: <b> 1 </b>, new name: <b> new-name </b>"))
                    || (routeId != 1 && m.Equals($"Cannot find the route id: <b> {routeId} </b>"))),
                It.IsAny<ParseMode>(), 
                It.IsAny<bool>(), 
                It.IsAny<bool>(), 
                It.IsAny<int>(), 
                It.IsAny<IReplyMarkup>(), 
                It.IsAny<CancellationToken>()));

            await using var dc2 = dcFactory.Create();
            {
                var route0Count = await dc2.GetTable<YasRoute>().Where(r =>r.RouteName=="new-name").CountAsync();
                var route1Count = await dc2.GetTable<YasRoute>().Where(r =>r.RouteName=="route-1").CountAsync();
                var route2Count = await dc2.GetTable<YasRoute>().Where(r =>r.RouteName=="route-2").CountAsync();

                Assert.Equal(routeId == 1 ? 1 : 0, route0Count);
                Assert.Equal(routeId == 1 ? 0 : 1, route1Count);
                Assert.Equal(1, route2Count);
            }
        }
    }
}
