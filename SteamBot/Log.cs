using System;
using System.IO;
using System.Linq;

namespace SteamBot
{
    public sealed class Log : IDisposable
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

        #region Private readonly variables.
        private readonly StreamWriter fileWriter;
        private readonly string botName;
        private readonly LogLevel outputLevel;
        private readonly LogLevel fileLogLevel;
        private readonly ConsoleColor defaultColor = ConsoleColor.White;
        private readonly bool showBotName; //IDK y we allow people to change this before when it's set via the config file.
        #endregion

        private bool disposed = false;

        public Log(string logFile, string botName = "", bool sDName = true, LogLevel consoleLogLevel = LogLevel.Info, LogLevel fileLogLevel = LogLevel.Info)
        {
            Directory.CreateDirectory("logs");
            fileWriter = File.AppendText(Path.Combine("logs", logFile));
            fileWriter.AutoFlush = true;
            this.botName = botName;
            outputLevel = consoleLogLevel;
            this.fileLogLevel = fileLogLevel;
            Console.ForegroundColor = defaultColor;
            showBotName = sDName;
        }

        ~Log()
        {
            Dispose(false);
        }

        // This outputs a log entry of the level info.
        public void Info(string data, params object[] formatParams)
        {
            _OutputLine(LogLevel.Info, data, formatParams);
        }

        // This outputs a log entry of the level debug.
        public void Debug(string data, params object[] formatParams)
        {
            _OutputLine(LogLevel.Debug, data, formatParams);
        }

        // This outputs a log entry of the level success.
        public void Success(string data, params object[] formatParams)
        {
            _OutputLine(LogLevel.Success, data, formatParams);
        }

        // This outputs a log entry of the level warn.
        public void Warn(string data, params object[] formatParams)
        {
            _OutputLine(LogLevel.Warn, data, formatParams);
        }

        // This outputs a log entry of the level error.
        public void Error(string data, params object[] formatParams)
        {
            _OutputLine(LogLevel.Error, data, formatParams);
        }

        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        public void Interface(string data, params object[] formatParams)
        {
            _OutputLine(LogLevel.Interface, data, formatParams);
        }

        // Outputs a line to both the log and the console, if
        // applicable.
        private void _OutputLine(LogLevel level, string line, object[] formatParams)
        {
            if (disposed)
                throw new ObjectDisposedException("Log");
            string formattedString = String.Format(
                "[{0}{1}] {2}: {3}",
                GetLogBotName(),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                level.ToString().ToUpper(), (formatParams != null && formatParams.Any() ? String.Format(line, formatParams) : line)
                );

            if (level >= fileLogLevel)
                fileWriter.WriteLine(formattedString);
            if (level >= outputLevel)
                _OutputLineToConsole(level, formattedString);
        }

        private string GetLogBotName()
        {
            if (botName == null)
                return "(System) ";
            else if (showBotName)
                return botName + " ";
            return "";
        }

        // Outputs a line to the console, with the correct color
        // formatting.
        private void _OutputLineToConsole(LogLevel level, string line)
        {
            Console.ForegroundColor = _LogColor(level);
            Console.WriteLine(line);
            Console.ForegroundColor = defaultColor;
        }

        // Determine the color to be used when outputting to the
        // console.
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
                    return defaultColor;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
                fileWriter.Dispose();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}