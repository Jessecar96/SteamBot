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
                Console.WriteLine ("Getting {0}...", options.Runner);
                var runner = (IBotRunner) System.Activator.CreateInstance(Type.GetType(options.Runner, true));
                runner.Start (options);
                //runner.DoLog (ELogType.ERROR, "Hello, World!");
                runner.DoLog (ELogType.DEBUG, "Debug");
                runner.DoLog (ELogType.INFO, "Info");
                runner.DoLog (ELogType.SUCCESS, "Success");
                runner.DoLog (ELogType.WARN, "Warn");
                runner.DoLog (ELogType.ERROR, "Error");
                runner.DoLog (ELogType.INTERFACE, "Interface");
                runner.DoLog (ELogType.NOTHING, "Nothing");
            }
            Console.ReadKey ();
		}
	}
}
