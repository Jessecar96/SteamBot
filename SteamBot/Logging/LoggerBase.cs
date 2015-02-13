﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.Logging
{
    public class LoggerParams
    {
        /// <summary>
        /// The LogLevel this class records at, anything below it is not logged.
        /// </summary>
        public LogLevel OutputLevel { get; private set; }

        /// <summary>
        /// Name of the bot to record log as.
        /// </summary>
        public string BotName { get; private set; }

        /// <summary>
        /// Shall BotName be used in logging?
        /// </summary>
        public bool ShowBotName { get; private set; }

        /// <summary>
        /// Format string to be used with String.Format
        /// </summary>
        public string Line { get; private set; }

        /// <summary>
        /// Parameters passed.
        /// </summary>
        public object[] FormatParams { get; private set; }

        public LoggerParams(LogLevel outputLevel, string botName, bool showBotName, string line, params object[] formatParams)
        {
            OutputLevel = outputLevel;
            BotName = botName;
            ShowBotName = showBotName;
            Line = line;
            FormatParams = formatParams;
        }

    }

    /// <summary>
    /// An abstract class for Log class.
    /// </summary>
    public abstract class LoggerBase
    {
        /// <summary>
        /// The min logging output level for this class.
        /// </summary>
        public LogLevel OutputLevel { get; set; }

        public LoggerBase(LogLevel outputLevel)
        {
            OutputLevel = outputLevel;
        }

        private string BotName(LoggerParams lParams)
        {
            if (!lParams.ShowBotName)
                return String.Empty;
            else if (String.IsNullOrEmpty(lParams.BotName))
                return "(System) ";
            else
                return String.Format("({0}) ", lParams.BotName);
        }

        protected virtual string FormatLine(LoggerParams lParams)
        {
            return String.Format(
                "[{0}{1}] {2}: {3}",
                BotName(lParams),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LoggingUtils.LogLevelStr(lParams.OutputLevel).ToUpper(), (lParams.FormatParams != null && lParams.FormatParams.Any() ? String.Format(lParams.Line, lParams.FormatParams) : lParams.Line)
                );
        }

        //Override this for your own logger class. Return true if it was a success;
        public abstract void LogMessage(LoggerParams lParams);
    }
}
