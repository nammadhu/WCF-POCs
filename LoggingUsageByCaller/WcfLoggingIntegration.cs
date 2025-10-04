using System;
using WCF_POCs.Logging;

namespace WCF_POCs.LoggingUsageByCaller
{
    /// <summary>
    /// Helper class to configure WCF logging with User infrastructure
    /// Call this during application startup ONLY if you want to use USER logging instead of console logging
    /// </summary>
    public static class USERWcfLoggingSetup
    {
        public static void Initialize()
        {
            try
            {
                // Replace default console logger with USER logger
                WcfLoggerFactory.SetLogger(new USERWcfLogger());
                Console.WriteLine("[INFO] WCF logging configured to use USER LoggerManager");
            }
            catch (Exception ex)
            {
                // Fallback to console logging if USER infrastructure is not available
                Console.WriteLine($"[WARN] USER logging infrastructure not available, using console logging: {ex.Message}");
                // WcfLoggerFactory will continue using DefaultConsoleLogger
            }
        }
    }

    /// <summary>
    /// USER-specific implementation of WCF logging interfaces
    /// This bridges the generic WCF logging extension with USER's logging infrastructure
    /// </summary>
    internal class USERWcfLogger : IWcfLogger
    {
        public void LogError(string message, Exception ex = null)
        {
            try
            {
                //currently defaulted to ConsoleLogger, but user can add any other of own internal choice
                ConsoleLoggerError();
            }
            catch
            {
                // Fallback to console if USER logger fails
                ConsoleLoggerError();
            }

            void ConsoleLoggerError()
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                if (ex != null)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
                Console.ResetColor();
            }
        }

        public void LogWarning(string message)
        {
            try
            {
                //currently defaulted to ConsoleLogger, but user can add any other of own internal choice
                ConsoleLoggerWarning();
            }
            catch
            {
                // Fallback to console if USER logger fails
                ConsoleLoggerWarning();
            }

            void ConsoleLoggerWarning()
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                Console.ResetColor();
            }
        }

        public void LogInfo(string message)
        {
            try
            {
                //currently defaulted to ConsoleLogger, but user can add any other of own internal choice
                ConsoleLogger();
            }
            catch
            {
                // Fallback to console if USER logger fails
                ConsoleLogger();
            }

            void ConsoleLogger()
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                Console.ResetColor();
            }
        }

        public void LogDebug(string message)
        {
            try
            {
                //currently defaulted to ConsoleLogger, but user can add any other of own internal choice
                ConsoleLoggerDebug();
            }
            catch
            {
                // Fallback to console if USER logger fails
                ConsoleLoggerDebug();
            }

            void ConsoleLoggerDebug()
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                Console.ResetColor();
            }
        }
    }
}