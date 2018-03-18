using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;

namespace SteamBot.TF2GC
{
    public enum ECraftingRecipe : short
    {
        SmeltClassWeapons = 3,
        CombineScrap = 4,
        CombineReclaimed = 5,
        SmeltReclaimed = 22,
        SmeltRefined = 23
    }

    public static class Crafting
    {
        /// <summary>
        /// Crafts the specified items using the best fit recipe.
        /// </summary>
        /// <param name="bot">The current Bot</param>
        /// <param name="items">A list of ulong Item IDs to craft</param>
        /// <remarks>
        /// You must have set the current game to 440 for this to do anything.
        /// </remarks>
        public static void CraftItems(Bot bot, params ulong[] items)
        {
            CraftItems(bot, -2, items);
        }
        /// <summary>
        /// Crafts the specified items using the specified recipe.
        /// </summary>
        /// <param name="bot">The current Bot</param>
        /// <param name="recipe">The recipe number (unknown atm; -2 is "best fit")</param>
        /// <param name="items">A list of ulong Item IDs to craft</param>
        /// <remarks>
        /// You must have set the current game to 440 for this to do anything.
        /// </remarks>
        public static void CraftItems(Bot bot, ECraftingRecipe recipe, params ulong[] items)
        {
            CraftItems(bot, (short)recipe, items);
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
