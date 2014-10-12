using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SteamBot.Logging
{
    class FileLogger : LoggerBase, IDisposable
    {
        private StreamWriter FileWriter;

        private bool Disposed { get; set; }

        public FileLogger(LogLevel outputLevel, string logFile) : base(outputLevel)
        {
            Directory.CreateDirectory(Path.Combine(System.Windows.Forms.Application.StartupPath, "logs"));
            FileWriter = File.AppendText(Path.Combine("logs", logFile));
            FileWriter.AutoFlush = true;
        }

        public override void LogMessage(LoggerParams lParams)
        {
            if (Disposed)
                throw new ObjectDisposedException("This logger object is disposed");
            string formattedOutput = FormatLine(lParams);
            if (OutputLevel <= lParams.OutputLevel && FileWriter != null)
                FileWriter.WriteLine(formattedOutput);
        }

        public void Dispose()
        {
            FileWriter.Dispose();
            FileWriter = null;
            Disposed = true;
        }
    }
}
