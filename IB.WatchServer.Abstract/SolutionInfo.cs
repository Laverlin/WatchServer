using System;
using System.Reflection;

namespace IB.WatchServer.Abstract
{
    /// <summary>
    /// Helper class to get assembly and solution level info
    /// </summary>
    public static class SolutionInfo
    {
        private static readonly Lazy<string> _version = new Lazy<string>(()=>
            typeof(SolutionInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

        /// <summary>
        /// Application version info
        /// </summary>
        public static string Version => _version.Value;

        /// <summary>
        /// Assembly name
        /// </summary>
        public static string Name => typeof(SolutionInfo).Assembly.GetName().Name;
    }
}
