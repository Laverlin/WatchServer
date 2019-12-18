using System.Reflection;

namespace IB.WatchServer.Service.Infrastructure
{
    /// <summary>
    /// Helper class to get assembly and solution level info
    /// </summary>
    public static class SolutionInfo
    {
        /// <summary>
        /// Assembly version info
        /// </summary>
        /// <returns>string with assembly version</returns>
        public static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        internal static string GetName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }
    }
}
