using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace SteamBot.Logging
{
    public class FileLogger : LoggerBase, IDisposable
    {
        private bool disposed { get; set; }
        private readonly StreamWriter fileWriter;

        public FileLogger(JObject obj)
            : base(obj)
        {
            if (!Directory.Exists("log"))
                Directory.CreateDirectory("log");
            fileWriter = File.AppendText(Path.Combine("log", (string)obj["LogFile"]));
        }

        ~FileLogger()
        {
            DisposeFileLogger();
        }

        public override void LogMessage(LogParams lParams)
        {
            if (lParams.OutputLevel < OutputLevel)
                return;
            fileWriter.WriteLine(FormatLogMessage(lParams));
        }

        public void Dispose()
        {
            DisposeFileLogger();
            GC.SuppressFinalize(this); //Were already disposed of, y let GC make us redispose?.
        }

        private void DisposeFileLogger()
        {
            if (disposed)
                return;
            disposed = true;
            fileWriter.Dispose();
        }
    }
}
