using System;
using SteamKit2;
using SteamTrade;

namespace SteamBot
{
    /// <summary>
    /// A user handler class that implements basic text-based commands entered in
    /// chat or trade chat.
    /// </summary>
    public class AdminUserHandler : UserHandler
    {
        private const string AddCmd = "add";
        private const string AddCratesSubCmd = "crates";
        private const string AddWepsSubCmd = "weapons";
        private const string AddMetalSubCmd = "metal";
        private const string HelpCmd = "help";

        public AdminUserHandler(Bot bot, SteamID sid)
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
            Trade.SendMessage("Tell me what to do, Master. (Type " + HelpCmd + " for commands.)");
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
                PrintHelpMessage();
                return;
            }

            if (message.StartsWith(AddCmd))
                HandleAddCommand(message);

        }

        private void PrintHelpMessage()
        {
            Trade.SendMessage(String.Format("{0} {1} - adds all crates", AddCmd, AddCratesSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - adds all metal", AddCmd, AddMetalSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - adds all weapons", AddCmd, AddWepsSubCmd));
            Trade.SendMessage(String.Format(@"{0} <craft_material_type> [amount] - adds all or a given amount of items of a given crafing type.", AddCmd));
            Trade.SendMessage(String.Format(@"{0} <defindex> [amount] - adds all or a given amount of items of a given defindex.", AddCmd));

            Trade.SendMessage(@"See http://wiki.teamfortress.com/wiki/WebAPI/GetSchema for info about craft_material_type or defindex.");
        }

        private void HandleAddCommand(string command)
        {
            var data = command.Split(' ');

            if (data.Length < 2)
            {
                Trade.SendMessage("No parameter for cmd: " + AddCmd);
                return;
            }

            if (String.IsNullOrEmpty(data[1]))
            {
                Trade.SendMessage("No parameter for cmd: " + AddCmd);
                return;
            }

            uint amount = 0;
            if (data.Length > 2)
            {
                // get the optional ammount parameter
                if (!String.IsNullOrEmpty(data[2]))
                {
                    uint.TryParse(data[2], out amount);
                }
            }

            string typeToAdd = data[1];

            switch (typeToAdd)
            {
                case "metal":
                    AddItemsByCraftType("craft_bar", amount);
                    break;
                case "weapons":
                    AddItemsByCraftType("weapon", amount);
                    break;
                case "crates":
                    AddItemsByCraftType("supply_crate", amount);
                    break;
                default:
                    AddItemsByCraftType(typeToAdd, amount);
                    break;
            }
        }

        private void AddItemsByCraftType(string typeToAdd, uint amount)
        {
            var items = Trade.CurrentSchema.GetItemsByCraftingMaterial(typeToAdd);

            uint added = 0;

            foreach (var item in items)
            {
                added += Trade.AddAllItemsByDefindex(item.Defindex, amount);

                // if bulk adding something that has a lot of unique
                // defindex (weapons) we may over add so limit here also
                if (amount > 0 && added >= amount)
                    return;
            }
        }
    }
}