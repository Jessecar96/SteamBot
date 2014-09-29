using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SteamBot.Logging
{
    public class Log : IDisposable
    {
        public delegate void LogMessage(LoggerParams lParams);

        private string BotName = "";

        private bool ShowBotName;

        private List<LoggerBase> LoggerObjects;

        public event LogMessage OnLog;

        public Log(string botName, bool showBotName = true, params LoggerBase[] loggerObjects)
        {
            BotName = botName;
            LoggerObjects = new List<LoggerBase>();
            foreach (LoggerBase logger in loggerObjects)
            {
                LoggerObjects.Add(logger);
                logger.AddHandler(this);
            }
            ShowBotName = true;
        }

        private void CallLogMessage(LogLevel level, string data, params object[] formatParams)
        {
            LoggerParams logParams = new LoggerParams(level, BotName, ShowBotName, data, formatParams);
            OnLog(logParams);
        }

        // This outputs a log entry of the level info.
        public void Info(string data, params object[] formatParams)
        {
            CallLogMessage(LogLevel.Info, data, formatParams);
        }

        // This outputs a log entry of the level debug.
        public void Debug(string data, params object[] formatParams)
        {
            CallLogMessage(LogLevel.Debug, data, formatParams);
        }

        // This outputs a log entry of the level success.
        public void Success(string data, params object[] formatParams)
        {
            CallLogMessage(LogLevel.Success, data, formatParams);
        }

        // This outputs a log entry of the level warn.
        public void Warn(string data, params object[] formatParams)
        {
            CallLogMessage(LogLevel.Warn, data, formatParams);
        }

        // This outputs a log entry of the level error.
        public void Error(string data, params object[] formatParams)
        {
            CallLogMessage(LogLevel.Error, data, formatParams);
        }

        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        public void Interface(string data, params object[] formatParams)
        {
            CallLogMessage(LogLevel.Interface, data, formatParams);
        }

        public void AddLoggingObject(LoggerBase logger)
        {
            LoggerObjects.Add(logger);
            logger.AddHandler(this);
        }

        public void Dispose()
        {
            foreach (LoggerBase logger in LoggerObjects)
            {
                logger.Dispose();
            }
        }
    }
}

