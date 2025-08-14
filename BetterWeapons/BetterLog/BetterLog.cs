using HBS.Logging;
using System;
using System.IO;
using Object = UnityEngine.Object;

namespace BetterWeapons
{
    internal sealed class BetterLog : ILogAppender, IDisposable
    {
        private readonly StreamWriter streamWriter;
        private readonly BetterLogFormatter formatter;

        private BetterLog(string path, BetterLogSettings settings)
        {
            streamWriter = new StreamWriter(path) { AutoFlush = true };
            formatter = new BetterLogFormatter(settings.Formatter);
        }

        public void OnLogMessage(string logName, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            string formatted = formatter.GetFormattedLogLine(logName, level, message, context, exception, location);
            streamWriter.WriteLine(formatted);
        }

        public void Dispose()
        {
            streamWriter?.Dispose();
        }

        internal static BetterLogger SetupModLog(string path, string name, BetterLogSettings settings)
        {
            if (!settings.Enabled)
            {
                return new BetterLogger(null, LogLevel.Error);
            }

            ILog log = Logger.GetLogger(name);
            BetterLog appender = new(path, settings);
            Logger.AddAppender(name, appender);
            Logger.SetLoggerLevel(name, settings.Level);
            BetterLogger logger = new(log, settings.Level);
            return logger;
        }

        public void Flush()
        {
            streamWriter.Flush();
        }
    }
}