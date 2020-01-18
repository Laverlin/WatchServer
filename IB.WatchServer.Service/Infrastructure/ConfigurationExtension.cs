using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;

namespace IB.WatchServer.Service.Infrastructure
{
    public static class ConfigurationExtension
    {
        /// <summary>
        /// Load config for class TSettings from appsettings.json and validating it.
        /// The Class name is using as config section
        /// </summary>
        /// <typeparam name="TSettings">Type of settings class</typeparam>
        /// <param name="configuration">IConfiguration object<see cref="IConfiguration"/></param>
        public static TSettings LoadVerifiedConfiguration<TSettings>(this IConfiguration configuration)
            => LoadVerifiedConfiguration<TSettings, TSettings>(configuration);

        /// <summary>
        /// Load config for class TSettings from appsettings.json and validating it.
        /// The Class name is using as config section
        /// </summary>
        /// <typeparam name="ISettings">Contract of settings class</typeparam>
        /// <typeparam name="TSettings">Type of settings class</typeparam>
        /// <param name="configuration">IConfiguration object<see cref="IConfiguration"/></param>
        public static ISettings LoadVerifiedConfiguration<ISettings, TSettings>(this IConfiguration configuration)
            where TSettings : ISettings
        {
            var logger = Log.Logger.ForContext<Startup>();
            logger.Information($"validate :: { typeof(TSettings).Name }");
            TSettings settings;
            try
            {
                settings = configuration.GetSection(typeof(TSettings).Name).Get<TSettings>();
                Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
            }
            catch (Exception e)
            {
                logger.Error(e, $"{typeof(TSettings).Name} validation error");
                throw;
            }

            return settings;
        }
    }
}
