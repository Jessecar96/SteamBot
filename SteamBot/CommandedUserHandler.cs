using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamTrade;

namespace SteamBot
{
    /// <summary>
    /// A user handler class that implements basic text-based commands entered in
    /// chat or trade chat.
    /// </summary>
    public class CommandedUserHandler : UserHandler
    {
        private const string AddCratesCmd = "!addcrates";
        private const string AddWepsCmd = "!addweapons";
        private const string AddMetalCmd = "!addmetal";
        private const string AddCraftTypeCmd = "!addcrafttype";
        private const string HelpCmd = "!help";

        private static List<Schema.Item> weaponsToCraft;

        public CommandedUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
        }

        #region Overrides of UserHandler

        /// <summary>
        /// Called when a the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public override bool OnFriendAdd()
        {
            // if the other is an admin then accept add
            if (IsAdmin)
            {
                return true;
            }

            Log.Warn("Random SteamID: " + OtherSID + " tried to add the bot as a friend");
            return false;
        }

        public override void OnFriendRemove()
        {
        }

        /// <summary>
        /// Called whenever a message is sent to the bot.
        /// This is limited to regular and emote messages.
        /// </summary>
        public override void OnMessage(string message, EChatEntryType type)
        {
            // TODO: magic command system
        }

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public override bool OnTradeRequest()
        {
            if (IsAdmin)
                return true;

            return false;
        }

        public override void OnTradeError(string error)
        {
            Log.Error(error);
        }

        public override void OnTradeTimeout()
        {
            Log.Warn("Trade timed out.");
        }

        public override void OnTradeInit()
        {
            Trade.SendMessage("Success. Tell me what to do, Master. (Type !help for commands.)");
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // whatever.   
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // whatever.
        }

        public override void OnTradeMessage(string message)
        {
            ProcessTradeMessage(message);
        }

        public override void OnTradeReady(bool ready)
        {
            if (!IsAdmin)
            {
                Trade.SendMessage("You are not my master.");
                Trade.SetReady(false);
                return;
            }

            Trade.SetReady(true);
        }

        public override void OnTradeAccept()
        {
            if (IsAdmin)
            {
                bool ok = Trade.AcceptTrade();

                if (ok)
                {
                    Log.Success("Trade was Successful!");
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                }
            }
        }

        #endregion

        private void ProcessTradeMessage(string message)
        {
            if (message.Equals(HelpCmd))
            {
                Trade.SendMessage(AddCratesCmd + " - adds all crates.");
                Trade.SendMessage(AddMetalCmd + " - adds all metal.");
                Trade.SendMessage(AddWepsCmd + " - adds all weapons.");
                Trade.SendMessage(AddCraftTypeCmd + @" <craft_material_type> - adds all items of a given crafing type. See http://wiki.teamfortress.com/wiki/WebAPI/GetSchema");
            }

            if (message.Equals(AddCratesCmd))
                Trade.AddAllItemsByDefindex(5022);

            if (message.Equals(AddWepsCmd))
            {
                if (Trade.CurrentSchema != null && weaponsToCraft == null)
                {
                    weaponsToCraft = Trade.CurrentSchema.GetItemsByCraftingMaterial("weapon");
                }

                foreach (var weapon in weaponsToCraft)
                {
                    Trade.AddAllItemsByDefindex(weapon.Defindex);
                }
            }

            if (message.StartsWith(AddCraftTypeCmd))
            {
                var data = message.Split(' ');

                if (data.Length < 2)
                {
                    Trade.SendMessage("No parameter for cmd: " + AddCraftTypeCmd);
                    return;
                }

                if (String.IsNullOrEmpty(data[1]))
                {
                    Trade.SendMessage("No parameter for cmd: " + AddCraftTypeCmd);
                    return;
                }

                if (Trade.CurrentSchema != null)
                {
                    var items = Trade.CurrentSchema.GetItemsByCraftingMaterial(data[1]);

                    foreach (var item in items)
                    {
                        Trade.AddAllItemsByDefindex(item.Defindex);
                    }
                }
            }

            if (message.Equals(AddMetalCmd))
            {
                Trade.AddAllItemsByDefindex(5000);
                Trade.AddAllItemsByDefindex(5001);
                Trade.AddAllItemsByDefindex(5002);
            }
        }
    }
}