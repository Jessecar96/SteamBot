using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.Logging
{
    class ConsoleLogger : LoggerBase
    {
        private ConsoleColor DefaultConsoleColor;

        public ConsoleLogger(LogLevel outputLevel, ConsoleColor defaultColor = ConsoleColor.White) : base(outputLevel)
        {
            DefaultConsoleColor = defaultColor;
            Console.ForegroundColor = DefaultConsoleColor;
        }

        public override void LogMessage(LoggerParams lParams)
        {
            string formattedOutput = FormatLine(lParams);
            if (OutputLevel <= lParams.OutputLevel)
            {
                Console.ForegroundColor = _LogColor(lParams.OutputLevel);
                Console.WriteLine(formattedOutput);
                Console.ForegroundColor = DefaultConsoleColor;
            }
        }

        private ConsoleColor _LogColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    return ConsoleColor.White;
                case LogLevel.Success:
                    return ConsoleColor.Green;
                case LogLevel.Warn:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                case LogLevel.Interface:
                    return ConsoleColor.DarkCyan;
                default:
                    return DefaultConsoleColor;
            }
        }
    }
}
