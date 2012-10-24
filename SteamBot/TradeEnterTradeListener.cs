using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace SteamBot
{
    public class TradeEnterTradeListener : Trade.TradeListener
    {
        protected static void PrintConsole (String line, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine (line);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public int ScrapPutUp;

        protected Bot Bot;

        protected bool isAdmin {
            // Don't touch this, add admins to the settings.json file
            get { return Bot.Admins.Contains (trade.OtherSID); }
        }

        public TradeEnterTradeListener (Bot bot)
        {
            // Don't touch this
            Bot = bot;
        }

        public override void OnTimeout ()
        {
            // This is called when the other user is AFK

            Bot.SteamFriends.SendChatMessage (trade.OtherSID, EChatEntryType.ChatMsg,
                                     "Sorry, but you were AFK and the trade was canceled.");
            PrintConsole ("User was kicked because he was AFK.", ConsoleColor.Cyan);
        }

        public override void OnError (string message)
        {
            // This is called when there was a Steam Trading error
            Bot.SteamFriends.SendChatMessage(trade.OtherSID, EChatEntryType.ChatMsg,
                "Oh, there was an error: " + message + ". Maybe try again in a few minutes.");
            Console.WriteLine (message);
        }

        public override void OnAfterInit()
        {
            // This is called when a trade is done loading.  There should be a message saying the trade is ready. (Shown below)
            trade.SendMessage("Success.  Please put up your items.");
        }

        public override void OnUserAccept()
        {
            // This is called when the other user accepts
            if (Validate() || isAdmin)
            {
                dynamic js = trade.AcceptTrade();
                if (js.success == true)
                {
                    PrintConsole("[TradeSystem] Trade was successful!", ConsoleColor.Green);

                    // The trade has finished, You could log the trade to a file or database here.

                }
                else
                {
                    PrintConsole("[TradeSystem] Trade might have failed.", ConsoleColor.Red);
                }
            }
        }

        public override void OnUserSetReadyState (bool ready)
        {
            if (!ready) {
                // This is called when the other user is set not ready.
                // Currently, it makes the bot go not ready also.
                trade.SetReady (false);
            } else {
                // This is called when the other user is set ready.
                // You can remove the if Validate() to make the bot accept any items.  the method Valiate() is down below.
                if(Validate ()) {
                    trade.SetReady(true);
                }
            }
            trade.SendMessage("Scrap: " + ScrapPutUp);
        }

        public override void OnUserAddItem(Schema.Item schemaItem, Inventory.Item invItem)
        {
            // This is called when the other user adds an item to the trade
        }

        public override void OnUserRemoveItem(Schema.Item schemaItem, Inventory.Item invItem)
        {
            // This is called when the other user removes an item to the trade
        }

        public override void OnMessage(string message)
        {
            // This is called when the other user sends a chat message in the trade
        }

        public bool Validate ()
        {

            /*
             * This is called when the user accepts and is used to validate the items in the trade.
             * This must return a boolean weather the items are valid or not.
             * An example allowing only metal is shown below.
             */

            ScrapPutUp = 0;

            List<string> errors = new List<string> ();

            foreach (ulong id in trade.OtherOfferedItems) {
                var item = trade.OtherInventory.GetItem (id);
                if (item.Defindex == 5000)
                    ScrapPutUp++;
                else if (item.Defindex == 5001)
                    ScrapPutUp += 3;
                else if (item.Defindex == 5002)
                    ScrapPutUp += 9;
                else {
                    var schemaItem = Trade.CurrentSchema.GetItem (item.Defindex);
                    errors.Add ("Item " + schemaItem.Name + " is not a metal.");
                }
            }

            if (ScrapPutUp < 1) {
                errors.Add ("You must put up at least 1 scrap.");
            }

            // send the errors
            if (errors.Count != 0)
                trade.SendMessage("There were errors in your trade: ");
            foreach (string error in errors) {
                trade.SendMessage(error);
            }

            return errors.Count == 0;
        }

    }
}
