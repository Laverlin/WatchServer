using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Helper class to put some info in log output
    /// </summary>
    public class StartupHostedService : IHostedService
    {
        private readonly ILogger<StartupHostedService> _logger;

        public StartupHostedService(ILogger<StartupHostedService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// On start, validate all confg object and print sturtup info to log
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start: {app}, version: {version}",
                SolutionInfo.GetName(),
                SolutionInfo.GetVersion());

            await Task.CompletedTask;
        }

        // noop
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
