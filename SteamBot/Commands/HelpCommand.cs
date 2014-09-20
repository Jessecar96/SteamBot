using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.Commands
{
	class HelpCommand : CommandBase
	{
		public HelpCommand()
		{
			cmdName = "help";
			cmdDescription = "Displays this help command.";
			cmdArgs = new List<ArgumentInfo>();
			cmdArgs.Add(new ArgumentInfo("cmd", "Command to get more info on.", true));
			adminCMD = false;
			cmdType = CmdType.CmdType_Chat | CmdType.CmdType_Console | CmdType.CmdType_Trade;
		}

		public override bool OnCommand(CommandParams cParams)
		{
			UserHandler theBot = cParams.botHandler;
			CommandHandler cmdHandler = cParams.handler;
			if (cmdHandler.Cmds.Count <= 0)
			{
				cParams.reply.Add("No commands available.");
				return false;
			}
			if (cParams.args.Length == 0)
			{
				cParams.reply.Add(String.Format("To get more information about a command, do: help <help>{0}Example: help help", Environment.NewLine));
				foreach (CommandBase cmd in cParams.handler.Cmds)
				{
					if (!cmd.IsAdminCmd || (cmd.IsAdminCmd && theBot.IsAdmin))
					{
						if (cmd.IsOfType(cParams.cmdActivator))
							cParams.reply.Add(cmd.ToString());
					}
				}
			}
			else if (cParams.args.Length != 1)
			{
				cParams.reply.Add(String.Format("Invalid number of arguments passed, got {0} expected 1", cParams.args.Length));
				return false;
			}
			else
			{
				CommandBase matchedCmd = cParams.handler.FindCommand(cParams.args[0]);
				if (matchedCmd == null)
				{
					cParams.reply.Add(String.Format("Command {0} not found!", cParams.args[0]));
					return false;
				}
				else if (matchedCmd.IsAdminCmd && !theBot.IsAdmin)
				{
					cParams.reply.Add(String.Format("Command {0} can only be used by admins!", cParams.args[0]));
					return false;
				}
				else if (!matchedCmd.IsOfType(cParams.cmdActivator))
				{
					cParams.reply.Add(String.Format("Command {0} can't be used in this chat window!", cParams.args[0]));
					return false;
				}
				cParams.reply.Add(String.Format("Argument info for {0}", matchedCmd.CmdName));
				foreach (ArgumentInfo argInfo in matchedCmd.CmdArgs)
				{
					cParams.reply.Add(argInfo.ToString());
				}
			}
			return true;
		}
	}
}
