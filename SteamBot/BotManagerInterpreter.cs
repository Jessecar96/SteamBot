using System;
using System.IO;
using System.Collections.ObjectModel;

namespace SteamBot
{
    /// <summary>
    /// A interpreter for the bot manager so a user can control bots.
    /// </summary>
    /// <remarks>
    /// There is currently a small set of commands this can interpret. They 
    /// are limited by the functionality the <see cref="BotManager"/> class
    /// exposes.
    /// </remarks>
    public class BotManagerInterpreter
    {
        private readonly BotManager manager;
        private CommandSet p;
        private string start = String.Empty;
        private string stop = String.Empty;
        private bool showHelp;
        private bool clearConsole;

        public BotManagerInterpreter(BotManager manager)
        {
            this.manager = manager;
            p = new CommandSet
                    {
                        new BotManagerOption("stop", "stop (X) where X = the username or index of the configured bot",
                                             s => stop = s),
                        new BotManagerOption("start", "start (X) where X = the username or index of the configured bot",
                                             s => start = s),
                        new BotManagerOption("help", "shows this help text", s => showHelp = s != null),
                        new BotManagerOption("show",
                                             "show (x) where x is one of the following: index, \"bots\", or empty",
                                             param => ShowCommand(param)),
                        new BotManagerOption("clear", "clears this console", s => clearConsole = s != null),
                        new BotManagerOption("auth", "auth (X)=(Y) where X = the username or index of the configured bot and Y = the steamguard code",
                                             AuthSet),
                        new BotManagerOption("exec", 
                                             "exec (X) (Y) where X = the username or index of the bot and Y = your custom command to execute",
                                             ExecCommand),
                        new BotManagerOption("input",
                                             "input (X) (Y) where X = the username or index of the bot and Y = your input",
                                             InputCommand)
                    };
        }

        void AuthSet(string auth)
        {
            string[] xy = auth.Split('=');

            if (xy.Length == 2)
            {
                int index;
                string code = xy[1].Trim();

                if (int.TryParse(xy[0], out index) && (index < manager.ConfigObject.Bots.Length))
                {
                    Console.WriteLine("Authing bot with '" + code + "'");
                    manager.AuthBot(index, code);
                }
                else if (!String.IsNullOrEmpty(xy[0]))
                {
                    for (index = 0; index < manager.ConfigObject.Bots.Length; index++)
                    {
                        if (manager.ConfigObject.Bots[index].Username.Equals(xy[0], StringComparison.CurrentCultureIgnoreCase))
                        {
                            Console.WriteLine("Authing bot with '" + code + "'");
                            manager.AuthBot(index, code);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This interprets the given command string.
        /// </summary>
        /// <param name="command">The entire command string.</param>
        public void CommandInterpreter(string command)
        {
            showHelp = false;
            start = null;
            stop = null;

            p.Parse(command);

            if (showHelp)
            {
                Console.WriteLine("");
                p.WriteOptionDescriptions(Console.Out);
            }

            if (!String.IsNullOrEmpty(stop))
            {
                int index;
                if (int.TryParse(stop, out index) && (index < manager.ConfigObject.Bots.Length))
                {
                    manager.StopBot(index);
                }
                else
                {
                    manager.StopBot(stop);
                }
            }

            if (!String.IsNullOrEmpty(start))
            {
                int index;
                if (int.TryParse(start, out index) && (index < manager.ConfigObject.Bots.Length))
                {
                    manager.StartBot(index);
                }
                else
                {
                    manager.StartBot(start);
                }
            }

            if (clearConsole)
            {
                clearConsole = false;
                Console.Clear();
            }
        }

        private void ShowCommand(string param)
        {
            param = param.Trim();

            int i;
            if (int.TryParse(param, out i))
            {
                // spit out the bots config at index.
                if (manager.ConfigObject.Bots.Length > i)
                {
                    Console.WriteLine();
                    Console.WriteLine(manager.ConfigObject.Bots[i].ToString());
                }
            }
            else if (!String.IsNullOrEmpty(param))
            {
                if (param.Equals("bots"))
                {
                    // print out the config.Bots array
                    foreach (var b in manager.ConfigObject.Bots)
                    {
                        Console.WriteLine();
                        Console.WriteLine(b.ToString());
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                // print out entire config. 
                // the bots array does not get printed.
                Console.WriteLine();
                Console.WriteLine(manager.ConfigObject.ToString());
            }
        }

        private void ExecCommand(string cmd)
        {
            cmd = cmd.Trim();

            var cs = cmd.Split(' ');

            if (cs.Length < 2)
            {
                Console.WriteLine("Error: No command given to be executed.");
                return;
            }

            // Take the rest of the input as is
            var command = cmd.Remove(0, cs[0].Length + 1);

            int index;
            // Try index first then search usernames
            if (int.TryParse(cs[0], out index) && (index < manager.ConfigObject.Bots.Length))
            {
                if (manager.ConfigObject.Bots.Length > index)
                {
                    manager.SendCommand(index, command);
                    return;
                }
            }
            else if (!String.IsNullOrEmpty(cs[0]))
            {
                for (index = 0; index < manager.ConfigObject.Bots.Length; index++)
                {
                    if (manager.ConfigObject.Bots[index].Username.Equals(cs[0], StringComparison.CurrentCultureIgnoreCase))
                    {
                        manager.SendCommand(index, command);
                        return;
                    }
                }
            }
            // Print error
            Console.WriteLine("Error: Bot " + cs[0] + " not found.");
        }

        private void InputCommand(string inpt)
        {
            inpt = inpt.Trim();

            var cs = inpt.Split(' ');

            if (cs.Length < 2)
            {
                Console.WriteLine("Error: No input given.");
                return;
            }

            // Take the rest of the input as is
            var input = inpt.Remove(0, cs[0].Length + 1);

            int index;
            // Try index first then search usernames
            if (int.TryParse(cs[0], out index) && (index < manager.ConfigObject.Bots.Length))
            {
                if (manager.ConfigObject.Bots.Length > index)
                {
                    manager.SendInput(index, input);
                    return;
                }
            }
            else if (!String.IsNullOrEmpty(cs[0]))
            {
                for (index = 0; index < manager.ConfigObject.Bots.Length; index++)
                {
                    if (manager.ConfigObject.Bots[index].Username.Equals(cs[0], StringComparison.CurrentCultureIgnoreCase))
                    {
                        manager.SendInput(index, input);
                        return;
                    }
                }
            }
            // Print error
            Console.WriteLine("Error: Bot " + cs[0] + " not found.");
        }

        #region Nested Options classes
        // these are very much like the NDesk.Options but without the
        // maturity, features or need for command seprators like "-" or "/"

        private class BotManagerOption
        {
            public string Name { get; set; }
            public string Help { get; set; }
            public Action<string> Func { get; set; }

            public BotManagerOption(string name, string help, Action<string> func)
            {
                Name = name;
                Help = help;
                Func = func;
            }
        }

        private class CommandSet : KeyedCollection<string, BotManagerOption>
        {
            protected override string GetKeyForItem(BotManagerOption item)
            {
                return item.Name;
            }

            public void Parse(string commandLine)
            {
                var c = commandLine.Trim();

                var cs = c.Split(' ');

                foreach (var option in this)
                {
                    if (cs[0].Equals(option.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (cs.Length > 2)
                        {
                            option.Func(c.Remove(0, cs[0].Length + 1));
                        }
                        else if (cs.Length > 1)
                        {
                            option.Func(cs[1]);
                        }
                        else
                        {
                            option.Func(String.Empty);
                        }
                    }
                }
            }

            public void WriteOptionDescriptions(TextWriter o)
            {
                foreach (BotManagerOption p in this)
                {
                    o.Write('\t');
                    o.WriteLine(p.Name + '\t' + p.Help);
                }
            }
        }

        #endregion Nested Options classes
    }
}
