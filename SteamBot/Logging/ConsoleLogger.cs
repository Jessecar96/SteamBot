using System;
using Newtonsoft.Json.Linq;

namespace SteamBot.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        private readonly ConsoleColor defaultColor;
        public ConsoleLogger(JObject obj) : base(obj)
        {
            try
            {
                defaultColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), (string)obj["DefaultColor"]);
            }
            catch(Exception)
            {
                Console.WriteLine("Malformed console color detected. Defaulting to White");
                defaultColor = ConsoleColor.White;
            }
            Console.ForegroundColor = defaultColor;
        }

        private ConsoleColor getColor(LogLevel lvl)
        {
            switch (lvl)
            {
                case LogLevel.Error:
                    return ConsoleColor.Red;
                case LogLevel.Interface:
                    return ConsoleColor.Blue;
                case LogLevel.Success:
                    return ConsoleColor.Green;
                case LogLevel.Warn:
                    return ConsoleColor.Yellow;
                default:
                    return defaultColor;
            }
        }

        public override void LogMessage(LogParams lParams)
        {
            if (lParams.OutputLevel < OutputLevel)
                return;
            Console.ForegroundColor = getColor(lParams.OutputLevel);
            Console.WriteLine(FormatLogMessage(lParams));
            Console.ForegroundColor = defaultColor;
        }
    }
}
