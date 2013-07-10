using System;
using System.IO;

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
        protected string _Bot;
        public LogLevel OutputToConsole;
        public ConsoleColor DefaultConsoleColor = ConsoleColor.White;

        public Log (string logFile, string botName = "", LogLevel output = LogLevel.Info)
        {
            Directory.CreateDirectory(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "logs"));
            _FileStream = File.AppendText (System.IO.Path.Combine("logs",logFile));
            _FileStream.AutoFlush = true;
            _Bot = botName;
            OutputToConsole = output;
            Console.ForegroundColor = DefaultConsoleColor;
        }

        public void Dispose()
        {
            _FileStream.Dispose();
        }

        // This outputs a log entry of the level info.
        public void Info (string data)
        {
            _OutputLine (LogLevel.Info, data);
        }

        // This outputs a log entry of the level debug.
        public void Debug (string data)
        {
            _OutputLine (LogLevel.Debug, data);
        }

        // This outputs a log entry of the level success.
        public void Success (string data)
        {
            _OutputLine (LogLevel.Success, data);
        }

        // This outputs a log entry of the level warn.
        public void Warn (string data)
        {
            _OutputLine (LogLevel.Warn, data);
        }

        // This outputs a log entry of the level error.
        public void Error (string data)
        {
            _OutputLine (LogLevel.Error, data);
        }

        // This outputs a log entry of the level interface;
        // normally, this means that some sort of user interaction
        // is required.
        public void Interface (string data)
        {
            _OutputLine (LogLevel.Interface, data);
        }

        // Outputs a line to both the log and the console, if
        // applicable.
        protected void _OutputLine (LogLevel level, string line)
        {
            string formattedString = String.Format (
                "[{0} {1}] {2}: {3}",
                (_Bot == null ? "(System)" : _Bot),
                DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss"),
                _LogLevel (level).ToUpper (), line
                );
            _FileStream.WriteLine (formattedString);
            if (level >= OutputToConsole)
                _OutputLineToConsole (level, formattedString);
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
    }
}

