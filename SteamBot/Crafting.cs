using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;

namespace SteamBot
{
    public static class Crafting
    {
        //NOTE: The Steam Bot MUST have told Steam that it is in TF2 in order to do any TF2 related things.
        public static void CraftItems(Bot bot, params ulong[] items)
        {
            if (bot.CurrentGame != 440)
                throw new Exception("SteamBot is not ingame with AppID 440; current AppID is " + bot.CurrentGame);

            //-2 is "Wildcard"
            short recipe = -2;

            var craftMsg = new ClientGCMsg<MsgCraft>();

            craftMsg.Body.NumItems = (short)items.Length;
            craftMsg.Body.Recipe = recipe;

            foreach (ulong id in items)
                craftMsg.Write(id);

            bot.SteamGameCoordinator.Send(craftMsg, 440);
        }

        public static void CraftItems(Bot bot, short recipe, params ulong[] items)
        {
            if (bot.CurrentGame != 440)
                throw new Exception("SteamBot is not ingame with AppID 440; current AppID is " + bot.CurrentGame);

            var craftMsg = new ClientGCMsg<MsgCraft>();

            craftMsg.Body.NumItems = (short)items.Length;
            craftMsg.Body.Recipe = recipe;

            foreach (ulong id in items)
                craftMsg.Write(id);

            bot.SteamGameCoordinator.Send(craftMsg, 440);
        }
    }
}
