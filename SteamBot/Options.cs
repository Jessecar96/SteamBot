using System;
using CommandLine;

namespace SteamBot
{
    /// <summary>
    /// This is used by CommandLine to parse options from the command line.
    /// </summary>
    public class Options : CommandLineOptionsBase
    {
        [Option('f', "from", HelpText = "Settings file to read from.", 
                Required = true)]
        public string ReadFile { get; set; }

        [Option('l', "log", HelpText = "File to log to.",
                DefaultValue = "steambot.log")]
        public string LogFile { get; set; }

        [Option('e', "level", HelpText = "The level at which to log at.  Anything higher will be logged.",
                DefaultValue = ELogType.INFO)]
        public ELogType LogLevel { get; set; }

        [Option('r', "runner", HelpText = "The bot runner that SteamBot will use.",
                DefaultValue = "Runners.ConsoleBotRunner")]
        public string Runner { get; set; }

        [HelpOption]
        public string GetUsage() {

            return CommandLine.Text.HelpText.AutoBuild(this,
                (CommandLine.Text.HelpText current) => 
                    CommandLine.Text.HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}

