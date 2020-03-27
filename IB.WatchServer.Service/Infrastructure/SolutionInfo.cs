using System;
using System.Reflection;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Helper class to get assembly and solution level info
    /// </summary>
    public static class SolutionInfo
    {
        /// <summary>
        /// Application version info
        /// </summary>
        public static string Version => (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        
        /// <summary>
        /// Assembly name
        /// </summary>
        public static string Name => Assembly.GetExecutingAssembly().GetName().Name;
    }
}
