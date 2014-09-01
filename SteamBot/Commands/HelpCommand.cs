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
			cmdType = CommandType.TypeBoth;
		}

		public override bool OnCommand(CommandParams cParams)
		{
			if (cParams.handler.Cmds.Count <= 0)
			{
				cParams.reply.Add("No commands available.");
				return false;
			}
			if (cParams.args.Length == 0)
			{
				cParams.reply.Add(String.Format("To get more information about a command, do: help <help>{0}Example: help help", Environment.NewLine));
				foreach (CommandBase cmd in cParams.handler.Cmds)
				{
					if (!cmd.IsAdminCmd || (cmd.IsAdminCmd && cParams.isAdmin))
					{
						if (cmd.CmdType == CommandType.TypeBoth || (cParams.isTrade && cmd.IsTradeCommand) || (!cParams.isTrade && cmd.IsChatCommand))
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
				else if (matchedCmd.IsAdminCmd && !cParams.isAdmin)
				{
					cParams.reply.Add(String.Format("Command {0} can only be used by admins!", cParams.args[0]));
					return false;
				}
				else if ((cParams.isTrade && matchedCmd.IsChatCommand) || (!cParams.isTrade && matchedCmd.IsTradeCommand))
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
