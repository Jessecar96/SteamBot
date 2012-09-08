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
			get { return Bot.Admins.Contains (trade.OtherSID); }
		}

		public TradeEnterTradeListener (Bot bot)
		{
			Bot = bot;
		}

		public override void OnTimeout ()
		{
			Bot.SteamFriends.SendChatMessage (trade.OtherSID, EChatEntryType.ChatMsg,
			                         "Sorry, but you were AFK and the trade was canceled.");
			PrintConsole ("User was kicked because he was AFK.", ConsoleColor.Cyan);
		}

		public override void OnError (string message)
		{
			Bot.SteamFriends.SendChatMessage(trade.OtherSID, EChatEntryType.ChatMsg,
				"Oh, there was an error: " + message + ". Maybe try again in a few minutes.");
			Console.WriteLine (message);
		}

		public override void OnAfterInit()
        {
            trade.SendMessage("Success. Trade me metal to get a ticket; 1 scrap is 1 ticket.");
        }

        public override void OnUserAccept()
        {
            if (Validate() || isAdmin) 
            {
                dynamic js = trade.AcceptTrade();
                if (js.success == true)
                {
                    PrintConsole("[TradeSystem] Trade was successful!", ConsoleColor.Green);

					// Here you would connect to a database and save the user and
					// how many tickets he bought.
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
				trade.SetReady (false);
			} else {
				if(Validate ()) {
					trade.SetReady(true);
				}
			}
			trade.SendMessage("Scrap: " + ScrapPutUp);
        }

        public override void OnUserAddItem(Schema.Item schemaItem, Inventory.Item invItem)
        {
			// do nothing
        }

        public override void OnUserRemoveItem(Schema.Item schemaItem, Inventory.Item invItem)
        {
			// do nothing
        }

        public override void OnMessage(string message)
        {

            // ignore chat messages
        }

        public bool Validate ()
		{
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
