using SteamKit2;
using System.Collections.Generic;
using SteamTrade;

namespace SteamBot
{
    public class SimpleUserHandler : UserHandler
    {
        public int ScrapPutUp;

        public SimpleUserHandler (Bot bot, SteamID sid) : base(bot, sid) {}

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
            Trade.SendMessage ("Success. Please put up your items.");
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeMessage (string message) {}
        
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
                Trade.SendMessage ("Scrap: " + ScrapPutUp);
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
            ScrapPutUp = 0;
            
            List<string> errors = new List<string> ();
            
            foreach (ulong id in Trade.OtherOfferedItems)
            {
                var item = Trade.OtherInventory.GetItem (id);
                if (item.Defindex == 5000)
                    ScrapPutUp++;
                else if (item.Defindex == 5001)
                    ScrapPutUp += 3;
                else if (item.Defindex == 5002)
                    ScrapPutUp += 9;
                else
                {
                    var schemaItem = Trade.CurrentSchema.GetItem (item.Defindex);
                    errors.Add ("Item " + schemaItem.Name + " is not a metal.");
                }
            }
            
            if (ScrapPutUp < 1)
            {
                errors.Add ("You must put up at least 1 scrap.");
            }
            
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

