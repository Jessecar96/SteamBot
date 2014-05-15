using SteamKit2;
using System.Collections.Generic;
using SteamTrade;

namespace SteamBot
{
    public class SimpleUserHandler : UserHandler
    {
        // Add or remove as required
        TF2Inventory MyTF2Inventory;
        TF2Inventory OtherTF2Inventory;
        Dota2Inventory MyDota2Inventory;
        Dota2Inventory OtherDota2Inventory;

        public SimpleUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            MyTF2Inventory = TF2Inventory.FetchInventory(MySID, bot.apiKey);
            OtherTF2Inventory = TF2Inventory.FetchInventory(OtherSID, bot.apiKey);
            MyDota2Inventory = Dota2Inventory.FetchInventory(MySID, bot.apiKey);
            OtherDota2Inventory = Dota2Inventory.FetchInventory(OtherSID, bot.apiKey);
        }

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd()
        {
            return false;
        }

        public override void OnLoginCompleted()
        {
            // Planning on trading items from certain games?
            // Create or remove schema instances as necessary.
            Log.Info("Fetching TF2 schema...");
            TF2Schema.Schema = TF2Schema.FetchSchema(Bot.apiKey);
            Log.Info("Finished fetching TF2 schema!");
            Log.Info("Fetching Dota 2 schema...");
            Dota2Schema.Schema = Dota2Schema.FetchSchema(Bot.apiKey);
            Log.Info("Finished fetching Dota 2 schema!");

            // Add which inventory types you'll be doing trades with.
            AddInventoriesToFetch(GenericInventory.InventoryTypes.TF2);
            AddInventoriesToFetch(GenericInventory.InventoryTypes.Dota2);
            AddInventoriesToFetch(GenericInventory.InventoryTypes.SpiralKnights);
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnFriendRemove() { }

        public override void OnMessage(string message, EChatEntryType type)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
        }

        public override bool OnTradeRequest()
        {
            if (IsAdmin) return true;
            return false;
        }

        public override void OnTradeError(string error)
        {
            Bot.SteamFriends.SendChatMessage(OtherSID,
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            Bot.log.Warn(error);

        }

        public override void OnTradeTimeout()
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeInit()
        {
            Trade.SendMessage("Trade successfully initialized.");
        }

        public override void OnTradeAddItem(GenericInventory.Inventory.Item inventoryItem)
        {
            try
            {
                Log.Info(inventoryItem.Name + " was added.");                
                switch (inventoryItem.AppId)
                {
                    case 440:
                        {
                            if (!OtherTF2Inventory.IsPrivate)
                            {
                                var item = OtherTF2Inventory.GetItem(inventoryItem.Id);
                                var schemaItem = TF2Schema.Schema.GetItem(item.Defindex);
                            }
                            else
                            {
                                // Item is from private inventory. Use 'inventoryItem' properties to get the info you need instead.
                            }
                            break;
                        }
                    case 570:
                        {
                            if (!OtherDota2Inventory.IsPrivate)
                            {
                                var item = OtherDota2Inventory.GetItem(inventoryItem.Id);
                                var schemaItem = Dota2Schema.Schema.GetItem(item.Defindex);
                            }
                            else
                            {
                                // Item is from private inventory. Use 'inventoryItem' properties to get the info you need instead.
                            }
                            break;
                        }
                    case 99900:
                        {
                            if (inventoryItem.IsCurrency)
                            {
                                // This is always the total amount added, not the amount just added
                                int amountAdded = inventoryItem.Amount;
                            }
                            break;
                        }
                }
            }
            catch
            {
                // User added an item from an app we are not interested in.
                // You need to attempt to access a property of inventoryItem for this exception to be thrown
            }
        }

        public override void OnTradeRemoveItem(GenericInventory.Inventory.Item inventoryItem)
        {
            try
            {
                Log.Info(inventoryItem.Name + " was removed.");
            }
            catch
            {
                // User removed an item from an app we are not interested in
                // You need to attempt to access a property of inventoryItem for this exception to be thrown
            }
        }

        public override void OnTradeMessage(string message)
        {

        }

        public override void OnTradeReady(bool ready)
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                if (Validate() | IsAdmin)
                {
                    Trade.SetReady(true);
                }
            }
        }

        public override void OnTradeSuccess()
        {
            // Trade completed successfully
            Log.Success("Trade Complete.");
        }

        public override void OnTradeAccept()
        {
            if (Validate() | IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try
                {
                    Trade.AcceptTrade();
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                }

                Log.Success("Trade Complete!");
            }
        }

        public bool Validate()
        {
            List<string> errors = new List<string>();
            errors.Add("This demo is meant to show you how to handle items from different games. Trade cannot be completed, unless you're an Admin.");

            // send the errors
            if (errors.Count != 0)
                Trade.SendMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                Trade.SendMessage(error);
            }

            return errors.Count == 0;
        }

    }

}