using SteamKit2;
using System.Collections.Generic;
using SteamTrade;

namespace SteamBot
{
    public class SteamTradeDemoHandler : UserHandler
    {
        // NEW ------------------------------------------------------------------
        private GenericInventory mySteamInventory = new GenericInventory();
        private GenericInventory OtherSteamInventory = new GenericInventory();
        // ----------------------------------------------------------------------

        public SteamTradeDemoHandler (Bot bot, SteamID sid) : base(bot, sid) {}

        public override bool OnFriendAdd () 
        {
            return true;
        }

        public override void OnLoginCompleted()
        {
        }

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
            // NEW -------------------------------------------------------------------------------
            List<uint> InvType = new List<uint>();

            /*************************************************************************************
             * 
             * SteamInventory AppId = 753 
             * 
             * Inventory types:
             *  1 = Gifts (Games), must be public on steam profile in order to work.
             *  6 = Trading Cards, Emoticons & Backgrounds. 
             *  
             ************************************************************************************/

            InvType.Add(1);
            InvType.Add(6);

            mySteamInventory.load(753, InvType, Bot.SteamClient.SteamID);
            OtherSteamInventory.load(753, InvType, OtherSID);

            if (!mySteamInventory.loaded)
            {
                Trade.SendMessage("Couldn't open your inventory, type 'errors' for more info.");
            }

            Trade.SendMessage("Type 'test' to start.");
            // -----------------------------------------------------------------------------------
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {
            // USELESS DEBUG MESSAGES -------------------------------------------------------------------------------
            Trade.SendMessage("Object AppID:" + inventoryItem.AppId);

            switch (inventoryItem.AppId)
            {
                case 440:
                    Trade.SendMessage("TF2 Item");
                    break;

                case 753:
                    GenericInventory.ItemDescription tmpDescription = OtherSteamInventory.getInfo(inventoryItem.Id);
                    Trade.SendMessage("Object type: " + tmpDescription.type);
                    Trade.SendMessage("Marketable: " + tmpDescription.marketable);
                    break;

                default:
                    Trade.SendMessage("Unknown item");
                    break;
            }
            // ------------------------------------------------------------------------------------------------------
        }
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeMessage (string message) {
            switch (message.ToLower())
            {
                case "errors":
                    foreach (string error in OtherSteamInventory.errors)
                    {
                        Trade.SendMessage(" * " + error);
                    }
                break;

                case "test":
                    Trade.SendMessage("Items on my bp: " + mySteamInventory.items.Count);
                    foreach(var item in mySteamInventory.items)
                    {
                        Trade.AddItem(item.Value.id,mySteamInventory.appId,item.Value.contextid);
                    }
                break;

                case "remove":
                    foreach (var item in mySteamInventory.items)
                    {
                        Trade.RemoveItem(item.Value.id, mySteamInventory.appId, item.Value.contextid);
                    }
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
                if(Validate ())
                {
                    Trade.SetReady (true);
                }
            }
        }
        
        public override void OnTradeAccept() 
        {
            if (Validate() || IsAdmin)
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

            OnTradeClose ();
        }

        public bool Validate ()
        {            
            List<string> errors = new List<string> ();
            errors.Add("This demo is meant to show you how to handle SteamInventory Items.");

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

