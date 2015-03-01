using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamBot.Logging
{
    public class Log : IDisposable
    {
        private bool disposed { get; set; }

        private readonly IEnumerable<LoggerBase> loggers;
        private readonly string botName;
        public bool ShowBotName { get; set; }

        public Log(string botName = null, bool showBotName = true, params LoggerBase[] loggers)
        {
            this.loggers = loggers;
            this.botName = botName;
            ShowBotName = String.IsNullOrWhiteSpace(botName) ? false : showBotName;
        }

        ~Log()
        {
            Dispose(false);
        }

        private void LogMessage(LogLevel lvl, string msg, params object[] fParams)
        {
            if (disposed)
                throw new ObjectDisposedException("Log");
            if (lvl == LogLevel.Nothing)
                return;
            LogParams lParams = new LogParams(msg, lvl, ShowBotName, botName, fParams);
            foreach (LoggerBase logger in loggers)
                logger.LogMessage(lParams);
        }

        public void Nothing(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Nothing, msg, fParams);
        }

        public void Debug(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Debug, msg, fParams);
        }

        public void Info(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Info, msg, fParams);
        }

        public void Success(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Success, msg, fParams);
        }

        public void Warn(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Warn, msg, fParams);
        }

        public void Error(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Error, msg, fParams);
        }

        public void Interface(string msg, params object[] fParams)
        {
            LogMessage(LogLevel.Interface, msg, fParams);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); //Were already disposed of, y let GC make us redispose?.
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;
            if (disposing)
            {
                //Loggers are to be considered MANAGED resources.
                //Any logger instance that isn't ConsoleLogger or FileLogger that leak memory is at fault of it's coder.
                foreach (IDisposable disposableLogger in loggers.OfType<IDisposable>())
                    disposableLogger.Dispose();
            }
        }
    }
}

