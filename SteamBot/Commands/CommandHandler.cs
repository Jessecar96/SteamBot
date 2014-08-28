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

		private bool CanFireCommand(CommandBase cmd, bool isAdmin, bool isTrade)
		{
			bool ret = true;
			if (cmd.CmdType != CommandType.TypeBoth)
			{
				if (isTrade && cmd.IsChatCommand)
					ret = false;
				else if (!isTrade && cmd.IsTradeCommand)
					ret = false;
			}
			if (!isAdmin && cmd.IsAdminCmd)
				ret = false;
			return ret;
		}

		private void ReplyToCommand(bool isTrade, Bot theBot, SteamID user, string message)
		{
			if (isTrade)
				theBot.CurrentTrade.SendMessage(message);
			else
				theBot.SteamFriends.SendChatMessage(user, EChatEntryType.ChatMsg, message);
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
		public void OnMessage(string message, Bot theBot, SteamID user, bool isTrade)
		{;
			bool isAdmin = theBot.Admins.Contains(user);
			string[] splitMSG = message.Split(' ');
			foreach (CommandBase cmd in cmds)
			{
				if (CanFireCommand(cmd, isAdmin, isTrade) && splitMSG[0] == cmd.CmdName)
				{
					string[] args = new string[splitMSG.Length - 1];
					for (int i = 1; i < splitMSG.Length; i++)
					{
						args[i - 1] = splitMSG[i];
					}
					CommandParams cParams = new CommandParams(this, user, isTrade, isAdmin, args);
					if (!cmd.OnCommand(ref cParams))
						ReplyToCommand(isTrade, theBot, user, "The following errors occurred when proccessing a command:");
					foreach (string msg in cParams.reply)
					{
						ReplyToCommand(isTrade, theBot, user, msg);
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
