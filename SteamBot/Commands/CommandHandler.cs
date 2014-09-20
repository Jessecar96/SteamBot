using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot.Commands
{
	/// <summary>
	/// Class to handle commands.
	/// </summary>
	public class CommandHandler
	{
		private List<CommandBase> cmds;

		public CommandHandler()
		{
			cmds = new List<CommandBase>();
			AddCommand(new HelpCommand());
		}

		/// <summary>
		/// Find a command in list of commands by name.
		/// </summary>
		/// <param name="cmdName">Name of command to look for(ex: help)</param>
		/// <returns>The CommandBase object of command if found, null otherwise.</returns>
		public CommandBase FindCommand(string cmdName)
		{
			foreach (CommandBase cmd in cmds)
			{
				if (cmd.CmdName == cmdName)
				{
					return cmd;
				}
			}
			return null;
		}

		private bool CanFireCommand(CommandBase cmd, CmdType type, UserHandler theBot)
		{
			bool ret = true;
			if (!cmd.IsOfType(type))
				ret = false;
			if (!theBot.IsAdmin && cmd.IsAdminCmd)
				ret = false;
			return ret;
		}

		private void ReplyToCommand(CmdType type, UserHandler theBot, string message, bool isError)
		{
			if ((type & CmdType.CmdType_Trade) == CmdType.CmdType_Trade)
				theBot.Trade.SendMessage(message);
			else if ((type & CmdType.CmdType_Chat) == CmdType.CmdType_Chat)
				theBot.Bot.SteamFriends.SendChatMessage(theBot.OtherSID, EChatEntryType.ChatMsg, message);
			else if ((type & CmdType.CmdType_Console) == CmdType.CmdType_Console)
			{
				if (isError)
					theBot.Log.Error(message);
				else
					theBot.Log.Success(message);
			}
		}

		/// <summary>
		/// Add a command class to list of commands.
		/// </summary>
		/// <param name="cmd">A class that inherits CommandBase.</param>
		public void AddCommand(CommandBase cmd)
		{
			cmds.Add(cmd);
		}

		/// <summary>
		/// Call this when the bot recieves a trade chat message or steam chat message.
		/// </summary>
		/// <param name="message">Unmodified message bot got.</param>
		/// <param name="theBot">Bot object that your handler has.</param>
		/// <param name="user">The user OtherSID is assigned to.</param>
		/// <param name="isTrade">Set to true when calling this in callback for trade message.</param>
		public void OnMessage(string message, UserHandler theBot, CmdType type)
		{
			string[] splitMSG = message.Split(' ');
			foreach (CommandBase cmd in cmds)
			{
				if (CanFireCommand(cmd, type, theBot) && splitMSG[0] == cmd.CmdName)
				{
					string[] args = new string[splitMSG.Length - 1];
					for (int i = 1; i < splitMSG.Length; i++)
					{
						args[i - 1] = splitMSG[i];
					}
					CommandParams cParams = new CommandParams(this, theBot, args, type);
					bool cmdError = !cmd.OnCommand(cParams);
					if (cmdError)
						ReplyToCommand(type, theBot, "The following errors occurred when proccessing a command:", true);
					foreach (string msg in cParams.reply)
					{
						ReplyToCommand(type, theBot, msg, cmdError);
					}
				}
			}
		}

		/// <summary>
		/// The list of commands this handler has.
		/// </summary>
		public List<CommandBase> Cmds
		{
			get
			{
				return cmds;
			}
		}
	}
}
