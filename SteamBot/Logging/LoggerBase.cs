using System;
using Newtonsoft.Json.Linq;

namespace SteamBot.Logging
{
    public abstract class LoggerBase
    {
        protected readonly LogLevel OutputLevel;

        public LoggerBase(JObject obj)
        {
            OutputLevel = GetFromJson(obj, "LogLevel", LogLevel.Info);
        }

        protected virtual string FormatLogMessage(LogParams lParams)
        {
            return String.Format("[{0}{1}] {2}: {3}",
                lParams.ShowBotName ? lParams.BotName : String.Empty,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LogUtilities.LogLevelStr(lParams.OutputLevel).ToUpper(),
                String.Format(lParams.Message, lParams.FormatParams));
        }

        protected T GetFromJson<T>(JObject obj, string key, T defValue = default(T))
        {
            if (obj[key] == null)
                return defValue;
            try
            {
                return (T)Convert.ChangeType(obj[key], typeof(T));
            }
            catch (Exception)
            {
                return defValue;
            }
        }

        public abstract void LogMessage(LogParams lParams);
    }
}
