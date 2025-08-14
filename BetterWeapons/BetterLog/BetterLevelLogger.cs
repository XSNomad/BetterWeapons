using HBS.Logging;
using System;

namespace BetterWeapons
{
    public class BetterLevelLogger
    {
        private readonly ILog log;
        private readonly LogLevel level;

        public BetterLevelLogger(ILog log, LogLevel level)
        {
            this.log = log;
            this.level = level;
        }

        public void Log(object message)
        {
            log.LogAtLevel(level, message);
        }

        public void Log(object message, Exception e)
        {
            log.LogAtLevel(level, message, e);
        }

        public void Log(Func<string> callback)
        {
            log.LogAtLevel(level, callback());
        }
    }
}