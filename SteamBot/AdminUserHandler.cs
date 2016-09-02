﻿using System;
using System.Windows.Forms;
using SteamKit2;
using SteamTrade;
using System.Collections.Generic;
using SteamTrade.TradeOffer;

namespace SteamBot
{
    /// <summary>
    /// A user handler class that implements basic text-based commands entered in
    /// chat or trade chat.
    /// </summary>
    public class AdminUserHandler : UserHandler
    {
        private const string AddCmd = "add";
        private const string RemoveCmd = "remove";
        private const string AddCratesSubCmd = "crates";
        private const string AddWepsSubCmd = "weapons";
        private const string AddMetalSubCmd = "metal";
        private const string AddAllSubCmd = "all";
        private const string HelpCmd = "help";

        public AdminUserHandler(Bot bot, SteamID sid) : base(bot, sid) {}

        #region Overrides of UserHandler

        /// <summary>
        /// Called when the bot is fully logged in.
        /// </summary>
        public override void OnLoginCompleted()
        {
        }

        /// <summary>
        /// Triggered when a clan invites the bot.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public override bool OnGroupAdd()
        {
            return false;
        }

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
            SendTradeMessage("Success. (Type {0} for commands)", HelpCmd);
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
                SendTradeMessage("You are not my master.");
                Trade.SetReady(false);
                return;
            }

            Trade.SetReady(true);
        }

        public override void OnTradeOfferUpdated(TradeOffer offer)
        {
            if(offer.OfferState == TradeOfferState.TradeOfferStateAccepted)
            {
                Log.Success("Trade Complete.");
            }
        }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID)
        {
            Log.Warn("Trade ended awaiting confirmation");
            SendChatMessage("Please complete the confirmation to finish the trade");
        }

        public override void OnTradeAccept()
        {
            if (IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try
                {
                    if (Trade.AcceptTrade())
                        Log.Success("Trade Accepted!");
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
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
            {
                HandleAddCommand(message);
                SendTradeMessage("done adding.");
            }
            else if (message.StartsWith(RemoveCmd))
            {
                HandleRemoveCommand(message);
                SendTradeMessage("done removing.");
            }
        }

        private void PrintHelpMessage()
        {
            SendTradeMessage("{0} {1} [amount] [series] - adds all crates (optionally by series number, use 0 for amount to add all)", AddCmd, AddCratesSubCmd);
            SendTradeMessage("{0} {1} [amount] - adds metal", AddCmd, AddMetalSubCmd);
            SendTradeMessage("{0} {1} [amount] - adds weapons", AddCmd, AddWepsSubCmd);
            SendTradeMessage("{0} {1} [amount] - adds items", AddCmd, AddAllSubCmd);
            SendTradeMessage(@"{0} <craft_material_type> [amount] - adds all or a given amount of items of a given crafting type.", AddCmd);
            SendTradeMessage(@"{0} <defindex> [amount] - adds all or a given amount of items of a given defindex.", AddCmd);

            SendTradeMessage(@"See http://wiki.teamfortress.com/wiki/WebAPI/GetSchema for info about craft_material_type or defindex.");
        }

        private void HandleAddCommand(string command)
        {
            var data = command.Split(' ');
            string typeToAdd;

            bool subCmdOk = GetSubCommand (data, out typeToAdd);

            if (!subCmdOk)
                return;

            uint amount = GetAddAmount (data);

            // if user supplies the defindex directly use it to add.
            int defindex;
            if (int.TryParse(typeToAdd, out defindex))
            {
                Trade.AddAllItemsByDefindex(defindex, amount);
                return;
            }

            switch (typeToAdd)
            {
                case AddMetalSubCmd:
                    AddItemsByCraftType("craft_bar", amount);
                    break;
                case AddWepsSubCmd:
                    AddItemsByCraftType("weapon", amount);
                    break;
                case AddCratesSubCmd:
                    // data[3] is the optional series number
                    if (!String.IsNullOrEmpty(data[3]))
                        AddCrateBySeries(data[3], amount);
                    else
                        AddItemsByCraftType("supply_crate", amount);
                    break;
                case AddAllSubCmd:
                    AddAllItems();
                    break;
                default:
                    AddItemsByCraftType(typeToAdd, amount);
                    break;
            }
        }



        private void HandleRemoveCommand(string command)
        {
            var data = command.Split(' ');

            string subCommand;

            bool subCmdOk = GetSubCommand(data, out subCommand);

            // were dumb right now... just remove everything.
            Trade.RemoveAllItems();

            if (!subCmdOk)
                return;
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

        private void AddAllItems()
        {
            var items = Trade.CurrentSchema.GetItems();

            foreach (var item in items)
            {
                Trade.AddAllItemsByDefindex(item.Defindex, 0);
            }
        }

        private void AddCrateBySeries(string series, uint amount)
        {
            int ser;
            bool parsed = int.TryParse(series, out ser);

            if (!parsed)
                return;

            var l = Trade.CurrentSchema.GetItemsByCraftingMaterial("supply_crate");


            List<Inventory.Item> invItems = new List<Inventory.Item>();

            foreach (var schemaItem in l)
            {
                ushort defindex = schemaItem.Defindex;
                invItems.AddRange(Trade.MyInventory.GetItemsByDefindex(defindex));
            }

            uint added = 0;

            foreach (var item in invItems)
            {
                int crateNum = 0;
                for (int count = 0; count < item.Attributes.Length; count++)
                {
                    // FloatValue will give you the crate's series number
                    crateNum = (int) item.Attributes[count].FloatValue;

                    if (crateNum == ser)
                    {
                        bool ok = Trade.AddItem(item.Id);

                        if (ok)
                            added++;

                        // if bulk adding something that has a lot of unique
                        // defindex (weapons) we may over add so limit here also
                        if (amount > 0 && added >= amount)
                            return;
                    }
                }
            }
        }

        bool GetSubCommand (string[] data, out string subCommand)
        {
            if (data.Length < 2)
            {
                SendTradeMessage("No parameter for cmd");
                subCommand = null;
                return false;
            }

            if (String.IsNullOrEmpty (data [1]))
            {
                SendTradeMessage("No parameter for cmd");
                subCommand = null;
                return false;
            }

            subCommand = data [1];

            return true;
        }

        static uint GetAddAmount (string[] data)
        {
            uint amount = 0;

            if (data.Length > 2)
            {
                // get the optional amount parameter
                if (!String.IsNullOrEmpty (data [2]))
                {
                    uint.TryParse (data [2], out amount);
                }
            }

            return amount;
        }
    }
}