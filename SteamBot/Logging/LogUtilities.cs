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
    };

    public class LogParams
    {
        public readonly string BotName;
        public readonly bool ShowBotName;
        public readonly LogLevel OutputLevel;
        public readonly string Message;
        public readonly object[] FormatParams;

        internal LogParams(string message, LogLevel outputLevel, bool showBotName, string botName, params object[] formatParams)
        {
            Message = message;
            OutputLevel = outputLevel;
            ShowBotName = showBotName;
            BotName = botName;
            FormatParams = formatParams;
        }
    }

    public static class LogUtilities
    {
        public static string LogLevelStr(LogLevel lvl)
        {
            switch (lvl)
            {
                case LogLevel.Debug:
                    return "Debug";
                case LogLevel.Info:
                    return "Info";
                case LogLevel.Success:
                    return "Success";
                case LogLevel.Warn:
                    return "Warning";
                case LogLevel.Error:
                    return "Error";
                case LogLevel.Interface:
                    return "Interface";
                default:
                    return "";
            }
        }
    }
}
