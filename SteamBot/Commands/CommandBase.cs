using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot.Commands
{
	/// <summary>
	/// Enum to tell handler where this command can be used.
	/// </summary>
	public enum CommandType
	{
		/// <summary>
		/// Only usable in a steam chat.
		/// </summary>
		TypeChat=0,
		/// <summary>
		/// Usable in both steam chat and trade chat.
		/// </summary>
		TypeBoth=1,
		/// <summary>
		/// Only usable in trade chat.
		/// </summary>
		TypeTrade=2,
	}
	/// <summary>
	/// Class to handle things commands need.
	/// </summary>
	public class CommandParams
	{
		/// <summary>
		/// Command handler that handled this command.
		/// </summary>
		public CommandHandler handler { get; private set; }

		/// <summary>
		/// SteamID of command user.
		/// </summary>
		public SteamID userSID { get; private set; }

		/// <summary>
		/// Was command called in trade chat?
		/// </summary>
		public bool isTrade { get; private set; }

		/// <summary>
		/// Is command user an admin?
		/// </summary>
		public bool isAdmin { get; private set; }

		/// <summary>
		/// Arguments passed to command.
		/// </summary>
		public string[] args { get; private set; }

		/// <summary>
		/// A list of strings to reply to user with.
		/// </summary>
		public List<string> reply { get; set; }

		public CommandParams(CommandHandler Handler, SteamID UserSID, bool IsTrade, bool IsAdmin, string[] Args)
		{
			handler = Handler;
			userSID = UserSID;
			isTrade = IsTrade;
			isAdmin = IsAdmin;
			args = Args;
			reply = new List<string>();
		}
	}
	/// <summary>
	/// Base class for commands.
	/// </summary>
	public abstract class CommandBase
	{

		/// <summary>
		/// This class is for information about an arguement.
		/// </summary>
		public class ArgumentInfo
		{
			/// <summary>
			/// Name of argument.
			/// </summary>
			public string argName { get; private set; }

			/// <summary>
			/// Description of argument.
			/// </summary>
			public string argDesc { get; private set; }

			/// <summary>
			/// Is argument optional?
			/// </summary>
			public bool isOptional { get; private set; }

			public ArgumentInfo(string name, string desc, bool optional)
			{
				argName = name;
				argDesc = desc;
				isOptional = optional;
			}

			public ArgumentInfo(string name, string desc)
			{
				argName = name;
				argDesc = desc;
				isOptional = false;
			}

			public override string ToString()
			{
				string argNameF = argName;
				if (isOptional)
					argNameF = String.Format("{{{0}}}", argName);
				return String.Format("{0,-12}{1}", argNameF, argDesc);
			}
		}

		/// <summary>
		/// Name of command.
		/// </summary>
		protected string cmdName;

		/// <summary>
		/// Description of command.
		/// </summary>
		protected string cmdDescription;

		/// <summary>
		/// A list of arguments(name as key, description as value).
		/// </summary>
		protected List<ArgumentInfo> cmdArgs;

		/// <summary>
		/// Is command for admins only?
		/// </summary>
		protected bool adminCMD;

		/// <summary>
		/// Type of command.
		/// </summary>
		protected CommandType cmdType;

		public CommandBase() { }

		/// <summary>
		/// Called when a command is fired.
		/// </summary>
		/// <param name="cParams">a reference to the parameters for command.</param>
		/// <returns>True if command can be successfully used.</returns>
		public abstract bool OnCommand(ref CommandParams cParams);

		/// <summary>
		/// Name of command.
		/// </summary>
		public string CmdName
		{
			get
			{
				return cmdName;
			}
		}

		/// <summary>
		/// Description of command.
		/// </summary>
		public string CmdDescription
		{
			get
			{
				return cmdDescription;
			}
		}

		/// <summary>
		/// A list of arguments(name as key, description as value).
		/// </summary>
		public List<ArgumentInfo> CmdArgs
		{
			get
			{
				return cmdArgs;
			}
		}

		/// <summary>
		/// Is command for admins only?
		/// </summary>
		public bool IsAdminCmd
		{
			get
			{
				return adminCMD;
			}
		}

		/// <summary>
		/// Type of command.
		/// </summary>
		public CommandType CmdType
		{
			get
			{
				return cmdType;
			}
		}

		/// <summary>
		/// Is command trade chat only?
		/// </summary>
		public bool IsTradeCommand
		{
			get
			{
				return cmdType == CommandType.TypeTrade;
			}
		}

		/// <summary>
		/// Is command steam chat only?
		/// </summary>
		public bool IsChatCommand
		{
			get
			{
				return cmdType == CommandType.TypeChat;
			}
		}

		public override string ToString()
		{
			return String.Format("{0,-12}{1}", cmdName, cmdDescription);
		}
	}
}
