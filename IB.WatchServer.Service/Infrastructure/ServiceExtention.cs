using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace IB.WatchServer.Service.Infrastructure
{
    public static class ServiceExtention
    {
        /// <summary>
        /// Load config for class TSettings from appsettings.json, validating it and add to DI as singleton.
        /// The Class name is using as config section
        /// </summary>
        /// <typeparam name="TSettings">Type of settings class</typeparam>
        /// <param name="services">collection of services <see cref="IServiceCollection"/></param>
        public static TSettings AddConfiguration<TSettings>(this IServiceCollection services)
            => AddConfiguration<TSettings, TSettings>(services);

        /// <summary>
        /// Load config for class TSettings from appsettings.json, validating it and add to DI as singleton.
        /// The Class name is using as config section
        /// </summary>
        /// <typeparam name="ISettings">Contract of settings class</typeparam>
        /// <typeparam name="TSettings">Type of settings class</typeparam>
        /// <param name="services">collection of services <see cref="IServiceCollection"/></param>
        public static ISettings AddConfiguration<ISettings, TSettings>(this IServiceCollection services)
            where TSettings : ISettings
        {
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            if (loggerFactory != null)
            {
                var logger = loggerFactory.CreateLogger(typeof(ServiceExtention));
                logger.LogInformation($"validate :: { typeof(TSettings).Name }");
            }

            TSettings settings = configuration.GetSection(typeof(TSettings).Name).Get<TSettings>();
            Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
            services.AddSingleton(typeof(ISettings), settings);
            return settings;
        }
    }
}
