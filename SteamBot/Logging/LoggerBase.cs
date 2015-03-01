using System;
using Newtonsoft.Json.Linq;

namespace SteamBot.Logging
{
    public abstract class LoggerBase
    {
        protected readonly LogLevel OutputLevel;

        public LoggerBase(JObject obj)
        {
            try
            {
                OutputLevel = (LogLevel)Enum.Parse(typeof(LogLevel), (string)obj["LogLevel"]);
            }
            catch (Exception)
            {
                Console.WriteLine("Malformed loglevel recieved, defaulting to INFO");
                OutputLevel = LogLevel.Info;
            }
        }

        protected virtual string FormatLogMessage(LogParams lParams)
        {
            return String.Format("[{0}{1}] {2}: {3}",
                lParams.ShowBotName ? lParams.BotName : String.Empty,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LogUtilities.LogLevelStr(lParams.OutputLevel).ToUpper(),
                String.Format(lParams.Message, lParams.FormatParams));
        }

        public abstract void LogMessage(LogParams lParams);
    }
}
