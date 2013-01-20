using System;
using SteamKit2;
using CommandLine;
using SteamBot;

namespace SteamBot
{
	class MainClass
	{
		public static void Main (string[] args)
        {
            Options options = new Options ();
            if (CommandLineParser.Default.ParseArguments (args, options))
            {
                var runner = (IBotRunner) System.Activator.CreateInstance(Type.GetType(options.Runner, true));
                // We passed the options to runner, it's now up to the runner to deal with it.
                runner.Start (options);
            }
		}
	}
}
