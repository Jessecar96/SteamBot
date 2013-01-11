using System;
using System.IO;
using SteamBot;

namespace Runners
{
    /// <summary>
    /// Console bot runner.  Despite its name, it also logs to a file, as well.
    /// The log file(s) are taken from the arguments passed to the file.
    /// </summary>
    public class ConsoleBotRunner : IBotRunner
    {

        private ELogType LogLevel;

        protected StreamWriter fileStream;

        ~ConsoleBotRunner ()
        {
            fileStream.Close ();
        }

        public void Start (Options options) 
        {
            this.LogLevel = options.LogLevel;
            fileStream = File.AppendText (options.LogFile);
            fileStream.AutoFlush = true;
        }

        public void DoLog (ELogType type, string log)
        {
            DoLog (type, "(system)", log);
        }

        public void DoLog (ELogType type, string name, string log)
        {
            string formattedString = String.Format ("[{0} {1}] {2}: {3}", name,
                                DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss"),
                                type.ToString (), log);
            if (type >= this.LogLevel)
            {
                Console.WriteLine (formattedString);
            }

            fileStream.WriteLine (formattedString);
        }
    }
}

