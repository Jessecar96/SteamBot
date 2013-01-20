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
                DefaultValue = "settings.json")]
        public string ReadFile { get; set; }

        [Option('l', "log", HelpText = "File to log to.",
                DefaultValue = "steambot.log")]
        public string LogFile { get; set; }

#if DEBUG
        [Option('e', "level", HelpText = "The level at which to log at.  Anything higher will be logged.",
                DefaultValue = ELogType.DEBUG)]
#else
        [Option('e', "level", HelpText = "The level at which to log at.  Anything higher will be logged.",
                DefaultValue = ELogType.SUCCESS)]
#endif
        public ELogType LogLevel { get; set; }

        [Option('r', "runner", HelpText = "The bot runner that SteamBot will use.",
                DefaultValue = "SteamBot.Runners.BasicBotRunner")]
        public string Runner { get; set; }

        [Option('s', "skip", HelpText = "Skip caching the schema.",
                DefaultValue = false)]
        public bool SkipSchema { get; set; }

        [HelpOption]
        public string GetUsage() {
            return CommandLine.Text.HelpText.AutoBuild(this,
                (CommandLine.Text.HelpText current) => 
                    CommandLine.Text.HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}

