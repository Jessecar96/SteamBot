using SteamKit2;
using System;
using System.Collections.Generic;
using SteamTrade;

namespace SteamBot
{
    public class SteamTradeDemoHandler : UserHandler
    {
        AsyncGenericInventory MySteamInventory = new AsyncGenericInventory();
        AsyncGenericInventory OtherSteamInventory = new AsyncGenericInventory();
        AsyncGenericInventory OtherDota2Inventory = new AsyncGenericInventory();

        bool tested;
        bool ready = false;


        public SteamTradeDemoHandler (Bot bot, SteamID sid) : base(bot, sid) 
        {
            //Call InventoryLoaded when Inventory is loaded
            MySteamInventory.OnLoadCompleted += InventoryLoaded;
            OtherSteamInventory.OnLoadCompleted += InventoryLoaded;
            OtherDota2Inventory.OnLoadCompleted += InventoryLoaded;
        }

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd () 
        {
            return true;
        }

        public override void OnLoginCompleted() { }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnFriendRemove () {}
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
            Bot.SteamFriends.SendChatMessage(OtherSID, type, Bot.ChatResponse);
        }

        public override bool OnTradeRequest() 
        {
            return true;
        }
        
        public override void OnTradeError (string error) 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, 
                                              EChatEntryType.ChatMsg,
                                              "Oh, there was an error: " + error + "."
                                              );
            Bot.log.Warn (error);

        }
        
        public override void OnTradeTimeout () 
        {
            Bot.SteamFriends.SendChatMessage (OtherSID, EChatEntryType.ChatMsg,
                                              "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info ("User was kicked because he was AFK.");
        }
        
        public override void OnTradeInit() 
        {
            /*************************************************************************************
             * 
             * SteamInventory AppId = 753 
             * 
             *  Context Id      Description
             *      1           Gifts (Games), must be public on steam profile in order to work
             *                  (steamcommunity.com/my/edit/settings).
             *      3           Coupons
             *      6           Trading Cards, Emoticons & Backgrounds.
             *      7           Steam Holiday Sale 2013 Rewards
             *  
             ************************************************************************************/
            int[] ContextIds = new int[] { 1, 3, 6, 7 };
            tested = false;

            MySteamInventory.Load(Bot.SteamClient.SteamID, 753, ContextIds);
            OtherSteamInventory.Load(OtherSID, 753, ContextIds);
            OtherDota2Inventory.Load(OtherSID, 570);
            
            Trade.SendMessage("Loading inventories");
            // -----------------------------------------------------------------------------------
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {
            // USELESS DEBUG MESSAGES -------------------------------------------------------------------------------
            Trade.SendMessage("ID: " + inventoryItem.Id);
            Trade.SendMessage("App (Game) ID: " + inventoryItem.AppId);
            Trade.SendMessage("Context ID: " + inventoryItem.ContextId);

            if (!ready)
            {
                Trade.SendMessage("Inventory Not Loaded");
                return;
            }

            GenericInventory.ItemDescription tmpDescription;

            switch (inventoryItem.AppId)
            {
                case 440:
                    Trade.SendMessage("TF2 Item Added.");

                    if (schemaItem != null)
                        Trade.SendMessage("Name: " + schemaItem.Name);
                    
                    Trade.SendMessage("Quality: " + inventoryItem.Quality);
                    Trade.SendMessage("Level: " + inventoryItem.Level);
                    Trade.SendMessage("Craftable: " + (inventoryItem.IsNotCraftable?"No":"Yes"));
                    break;

                case 570://DOTA2
                    Trade.SendMessage("DOTA2 Item Added.");

                    tmpDescription = OtherDota2Inventory.GetDescription(inventoryItem);

                    if (tmpDescription == null)
                    {
                        Trade.SendMessage("Description Not Found?");
                        break;
                    }

                    foreach (GenericInventory.Attribute attribute in tmpDescription.Attributes)
                    {
                        switch (attribute.CategoryName)
                        {
                            case "Rarity":
                                Trade.SendMessage(attribute.Name + " item");
                                break;

                            case "Quality":
                                Trade.SendMessage("Quality: " + attribute.Name);
                                break;
                        }
                    }
                    tmpDescription.DebugAttributes();
                    break;

                case 753:
                    tmpDescription = OtherSteamInventory.GetDescription(inventoryItem);

                    Trade.SendMessage("Steam Inventory Item Added.");

                    if (tmpDescription == null)
                    {
                        Trade.SendMessage("Description Not Found?");
                        break;
                    }

                    Trade.SendMessage("Type: " + tmpDescription.Type);
                    Trade.SendMessage("Marketable: " + (tmpDescription.IsMarketable?"Yes":"No"));
                    break;

                default:
                    Trade.SendMessage("Unknown item");
                    break;
            }
            // ------------------------------------------------------------------------------------------------------
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeMessage (string message) 
        {
            switch (message.ToLower())
            {
                case "errors":
                    if (OtherSteamInventory.Errors.Count > 0)
                    {
                        Trade.SendMessage("User Steam Inventory Errors:");
                        foreach (string error in OtherSteamInventory.Errors)
                        {
                            Trade.SendMessage(" * " + error);
                        }
                    }

                    if (MySteamInventory.Errors.Count > 0)
                    {
                        Trade.SendMessage("Bot Steam Inventory Errors:");
                        foreach (string error in MySteamInventory.Errors)
                        {
                            Trade.SendMessage(" * " + error);
                        }
                    }

                    if (OtherDota2Inventory.Errors.Count > 0)
                    {
                        Trade.SendMessage("Dota2 Inventory Bot Errors:");
                        foreach (string error in MySteamInventory.Errors)
                        {
                            Trade.SendMessage(" * " + error);
                        }
                    }

                break;

                case "test":
                    if (!ready)
                    {
                        Trade.SendMessage("Inventory Not Loaded");
                        return;
                    }

                    if (tested)
                    {
                        foreach (GenericInventory.Item item in MySteamInventory.Items.Values)
                        {
                            Trade.RemoveItem(item);
                        }
                    }
                    else
                    {
                        Trade.SendMessage("Items on my bp: " + MySteamInventory.Items.Count);
                        foreach (GenericInventory.Item item in MySteamInventory.Items.Values)
                        {
                            Trade.AddItem(item);
                        }
                    }

                    tested = !tested;

                break;

                case "remove":
                    Trade.RemoveAllItems();
                    tested = false;
                break;
            }
        }
        
        public override void OnTradeReady (bool ready) 
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();
            if (!ready)
            {
                Trade.SetReady (false);
            }
            else
            {
                if (IsAdmin || Validate())
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
            if (IsAdmin || Validate())
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    Trade.AcceptTrade();
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }

                Log.Success ("Trade Complete!");
            }
        }

        public bool Validate ()
        {            
            List<string> errors = new List<string> ();
            errors.Add("This demo is meant to show you how to load and handle Items.");
            errors.Add("The trade cannot be completed, unless you're an Admin.");

            // send the errors
            if (errors.Count != 0)
                Trade.SendMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                Trade.SendMessage(error);
            }
            
            return errors.Count == 0;
        }

        void InventoryLoaded(object sender, EventArgs args)
        {
            if (MySteamInventory.IsLoaded && OtherSteamInventory.IsLoaded && OtherDota2Inventory.IsLoaded)
            {
                Trade.SendMessage("Ready!, Type 'test' to start.");
                ready = true;
            }

            if (MySteamInventory.Errors.Count > 0 || OtherSteamInventory.Errors.Count > 0 || OtherDota2Inventory.Errors.Count > 0)
            {
                Trade.SendMessage("Couldn't open Inventory, type 'errors' for more info.");
            }
        }
    }
 
}

