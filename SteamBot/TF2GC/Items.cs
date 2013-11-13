using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;
using SteamTrade;

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

        /// <summary>
        /// Sets an item's 1-based position in the backpack
        /// </summary>
        /// <param name="bot">The current Bot</param>
        /// <param name="item">The 64-bit Item ID to move</param>
        /// <param name="position">The 1-based integer position of the item</param>
        /// <remarks>
        /// You must have set the current game to 440 for this to do anything.
        /// </remarks>
        public static void SetItemPosition(Bot bot, SteamTrade.Inventory.Item item, short position)
        {
            if (bot.CurrentGame != 440)
                throw new Exception ("SteamBot is not ingame with AppID 440; current AppID is " + bot.CurrentGame);

            byte[] bPos = BitConverter.GetBytes (position);
            byte[] bClass = BitConverter.GetBytes (-32768);

            byte[] nInv = new byte[] { bPos[0], bPos[1], bClass[0], bClass[1] };

            uint newInventoryDescriptor = BitConverter.ToUInt32 (nInv, 0);

            var aMsg = new ClientGCMsg<MsgSetItemPosition> ();

            aMsg.Write ((long)item.Id);
            aMsg.Write ((uint)newInventoryDescriptor);

            bot.SteamGameCoordinator.Send (aMsg, 440);
        }

        public static void AutoSetItemPositions(Bot bot)
        {
            bot.GetInventory();
            List<int> usedPositions = new List<int>();
            List<Inventory.Item> itemsToBePlaced = new List<Inventory.Item>();
            List<int> freePositions = new List<int>();
            foreach (var s in bot.MyInventory.Items)
            {
                if (s.InventoryPosition != 0)
                {
                    usedPositions.Add(s.InventoryPosition);
                }
                else
                {
                    itemsToBePlaced.Add(s);
                }
            }
            if (bot.MyInventory.NumSlots > usedPositions.Count)
            {
                usedPositions.Sort();
                var missing = Enumerable.Range(1, (int)bot.MyInventory.NumSlots).Except(usedPositions);
                freePositions = missing.ToList();
                var freeSlots = bot.MyInventory.NumSlots - usedPositions.Count;
                if (itemsToBePlaced.Count < freeSlots)
                {
                    for (int i = 0; i < itemsToBePlaced.Count; i++)
                    {
                        SetItemPosition(bot, itemsToBePlaced[i], (short)freePositions[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < freePositions.Count; i++)
                    {
                        SetItemPosition(bot, itemsToBePlaced[i], (short)freePositions[i]);
                    }
                }
                bot.log.Success("Items placed in Inventory Successfully.");
            }
        }
    }
}
