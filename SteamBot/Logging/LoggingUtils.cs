using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Success,
        Warn,
        Error,
        Interface, // if the user needs to input something
        Nothing    // not recommended; it basically silences
        // the console output because nothing is
        // greater than it.  even if the bot needs
        // input, it won't be shown in the console.
    }
    class LoggingUtils
    {
        // Determine the string equivalent of the LogLevel.
        public static string LogLevelStr(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                    return "info";
                case LogLevel.Debug:
                    return "debug";
                case LogLevel.Success:
                    return "success";
                case LogLevel.Warn:
                    return "warn";
                case LogLevel.Error:
                    return "error";
                case LogLevel.Interface:
                    return "interface";
                case LogLevel.Nothing:
                    return "nothing";
                default:
                    return "undef";
            }
        }
    }
}
