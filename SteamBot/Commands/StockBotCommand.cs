using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot.Commands
{
	public class StockBotCommand : CommandBase
	{
		public delegate void StockBot(SteamID otherSID);

		private StockBot func;

		public StockBotCommand(StockBot botFunc)
		{
			func = botFunc;
			cmdName = "stockBot";
			cmdDescription = "Allows admins to stock bot.";
			cmdArgs = new List<ArgumentInfo>();
			adminCMD = true;
			cmdType = CommandType.TypeChat;
		}

		public override bool OnCommand(CommandParams cParams)
		{
			if (!cParams.isAdmin)
			{
				cParams.reply.Add("Command only usable by admins.");
				return false;
			}
			cParams.reply.Add("I sent a trade request.");
			func(cParams.userSID);
			return true;
		}
	}
}
