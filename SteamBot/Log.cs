using System;
using System.IO;
using System.Linq;

namespace SteamBot
{
    public class Log : IDisposable
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


        protected StreamWriter _FileStream;
        protected string _botName;
        private bool disposed;
        public LogLevel OutputLevel;
        public LogLevel FileLogLevel;
        public ConsoleColor DefaultConsoleColor = ConsoleColor.White;
        public bool ShowBotName { get; set; }

        public Log(string logFile, string botName = "", LogLevel consoleLogLevel = LogLevel.Info, LogLevel fileLogLevel = LogLevel.Info)
        {
            Directory.CreateDirectory(Path.Combine(System.Windows.Forms.Application.StartupPath, "logs"));
            _FileStream = File.AppendText (Path.Combine("logs",logFile));
            _FileStream.AutoFlush = true;
            _botName = botName;
            OutputLevel = consoleLogLevel;
            FileLogLevel = fileLogLevel;
            Console.ForegroundColor = DefaultConsoleColor;
            ShowBotName = true;
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
        protected void _OutputLine(LogLevel level, string line, params object[] formatParams)
        {
            if (disposed)
                return;
            string formattedString = String.Format(
                "[{0}{1}] {2}: {3}",
                GetLogBotName(),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                _LogLevel(level).ToUpper(), (formatParams != null && formatParams.Any() ? String.Format(line, formatParams) : line)
                );

            if(level >= FileLogLevel)
            {
                _FileStream.WriteLine(formattedString);
            }
            if(level >= OutputLevel)
            {
                _OutputLineToConsole(level, formattedString);
            }
        }

        private string GetLogBotName()
        {
            if(_botName == null)
            {
                return "(System) ";
            }
            else if(ShowBotName)
            {
                return _botName + " ";
            }
            return "";
        }

        // Outputs a line to the console, with the correct color
        // formatting.
        protected void _OutputLineToConsole (LogLevel level, string line)
        {
            Console.ForegroundColor = _LogColor (level);
            Console.WriteLine (line);
            Console.ForegroundColor = DefaultConsoleColor;
        }

        // Determine the string equivalent of the LogLevel.
        protected string _LogLevel (LogLevel level)
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

        // Determine the color to be used when outputting to the
        // console.
        protected ConsoleColor _LogColor (LogLevel level)
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

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
                _FileStream.Dispose();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

