using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;

namespace SteamBot.TF2GC
{
    public static class Items
    {
        /// <summary>
        /// Permanently deletes the specified item
        /// </summary>
        /// <param name="bot">The current Bot</param>
        /// <param name="item">The 64-bit Item ID to delete</param>
        /// <remarks>
        /// You must have set the current game to 440 for this to do anything.
        /// </remarks>
        public static void DeleteItem(Bot bot, ulong item)
        {
            if (bot.CurrentGame != 440)
                throw new Exception("SteamBot is not ingame with AppID 440; current AppID is " + bot.CurrentGame);

            var deleteMsg = new ClientGCMsg<MsgDelete>();

            deleteMsg.Write((ulong)item);

            bot.SteamGameCoordinator.Send(deleteMsg, 440);
        }
    }
}
