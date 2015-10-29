using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Xml;
using System.ServiceModel.Syndication;
using System;
using System.IO;
using System.Text;


namespace SteamBot
{
    public class SimpleUserHandler : UserHandler
    {
		

        public TF2Value AmountAdded;

        public SimpleUserHandler (Bot bot, SteamID sid) : base(bot, sid) {}

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd () 
        {
            return true;
        }

        public override void OnLoginCompleted()
        {
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
		{
			Log.Info (Bot.SteamFriends.GetFriendPersonaName (sender) + ": " + message);
			base.OnChatRoomMessage (chatID, sender, message);
			if (message.StartsWith ("!VDC")) 
			{
				string par1 = message.Remove (0, 5);
				GoogleSearch(par1, "https://developer.valvesoftware.com/" , chatID);
			}

			if (message.StartsWith ("You have a leak")) 
			{
				string output = "https://developer.valvesoftware.com/wiki/leak";
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, output);
			}
			if (message.StartsWith ("!TF2")) 
			{
				string par1 = message.Remove (0, 5);
				GoogleSearch(par1, "https://wiki.teamfortress.com/" , chatID);
		    }
			if (message.StartsWith ("!DEBUG_01")) 
			{
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "RETURN TO SENDER");
			}

			if (message.StartsWith ("!RSS")) 
			{
				string url = "http://fooblog.com/feed";
				XmlReader reader = XmlReader.Create(url);
				SyndicationFeed feed = SyndicationFeed.Load(reader);
				reader.Close();
				foreach (SyndicationItem item in feed.Items)
				{
					string subject = item.Title.Text;    
					string summary = item.Summary.Text;
					 
					Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, subject);
				}
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, "RETURN TO SENDER");
			}
			if (message.StartsWith ("!DEBUG_02")) 
			{
				Bot.SteamFriends.SendChatRoomMessage (103582791429594873, EChatEntryType.ChatMsg, "SEND TO:" + "103582791429594873");
			}

			if (message.StartsWith ("!IMP")) 
			{
				string[] words = message.Split(' ');
				string save = message.Substring (5, message.Length - 5);
				maps (save);
			}
			if (message.StartsWith ("!MAPS")) 
			{
				string path = @"logs\maps.log";
				// Open the file to read from.
				string readText = File.ReadAllText(path);
				Log.Interface (readText);
				Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, readText);

			}

			if (message.StartsWith ("!CLEAR")) 
			{
				string path = @"logs\maps.log";
				File.Delete(path);
				File.WriteAllText (path, Environment.NewLine);
			}

     }
		public void GoogleSearch(string par1 , string url, SteamID chatID) {
			
			WebClient client = new WebClient ();
			string search = "http://www.google.com.au/search?q=" + par1 + "+site:" + url;
			string httpdata = client.DownloadString (search);
			string[] suffix = httpdata.Split (new string[] { "<h3 class=\"r\"><a href=\"/url?q=" + url }, System.StringSplitOptions.None);
			Log.Info (httpdata);
			string suffix_string = suffix [1];
			string[] suffix_split = suffix_string.Split (new string[] { "&" }, System.StringSplitOptions.None);
			string page = suffix_split [0];
			string output = url + page;
			Bot.SteamFriends.SendChatRoomMessage (chatID, EChatEntryType.ChatMsg, output);
			Log.Info ("requested:" + output);
		}



        public override void OnFriendRemove () {}
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
			SendChatMessage(Bot.ChatResponse);


			if (message.StartsWith ("!TF2MAPS")) 
			{
				Bot.SteamFriends.JoinChat (new SteamID (103582791429594873));
			}
        }
			
		public static void maps(string map)
		{
			string path = @"logs\maps.log";

			// This text is added only once to the file.
			if (!File.Exists (path)) {
				// Create a file to write to.
				string createText = map;
				File.WriteAllText (path, createText + Environment.NewLine);
			} 
			else 
			{
				// This text is always added, making the file longer over time
				// if it is not deleted.
				string appendText = map + Environment.NewLine;
				File.AppendAllText (path, appendText);

				// Open the file to read from.
				string readText = File.ReadAllText (path);
				Console.WriteLine (readText);
			}
		}

        public override bool OnTradeRequest() 
        {
            return true;
        }
        
        public override void OnTradeError (string error) 
        {
            SendChatMessage("Oh, there was an error: {0}.", error);
            Log.Warn (error);
        }
        
        public override void OnTradeTimeout () 
        {
            SendChatMessage("Sorry, but you were AFK and the trade was canceled.");
            Log.Info ("User was kicked because he was AFK.");
        }
        
        public override void OnTradeInit() 
        {
            SendTradeMessage("Success. Please put up your items.");
        }
        
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) {}
        
        public override void OnTradeMessage (string message) {}
        
        public override void OnTradeReady (bool ready) 
        {
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
                SendTradeMessage("Scrap: {0}", AmountAdded.ScrapTotal);
            }
        }

        public override void OnTradeSuccess()
        {
            Log.Success("Trade Complete.");
        }

        public override void OnTradeAwaitingEmailConfirmation(long tradeOfferID)
        {
            Log.Warn("Trade ended awaiting email confirmation");
            SendChatMessage("Please complete the email confirmation to finish the trade");
        }

        public override void OnTradeAccept() 
        {
            if (Validate() || IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    if (Trade.AcceptTrade())
                        Log.Success("Trade Accepted!");
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }
            }
        }

        public bool Validate ()
        {            
            AmountAdded = TF2Value.Zero;
            
            List<string> errors = new List<string> ();
            
            foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
            {
                var item = Trade.OtherInventory.GetItem(asset.assetid);
                if (item.Defindex == 5000)
                    AmountAdded += TF2Value.Scrap;
                else if (item.Defindex == 5001)
                    AmountAdded += TF2Value.Reclaimed;
                else if (item.Defindex == 5002)
                    AmountAdded += TF2Value.Refined;
                else
                {
                    var schemaItem = Trade.CurrentSchema.GetItem (item.Defindex);
                    errors.Add ("Item " + schemaItem.Name + " is not a metal.");
                }
            }
            
            if (AmountAdded == TF2Value.Zero)
            {
                errors.Add ("You must put up at least 1 scrap.");
            }
            
            // send the errors
            if (errors.Count != 0)
                SendTradeMessage("There were errors in your trade: ");
            foreach (string error in errors)
            {
                SendTradeMessage(error);
            }
            
            return errors.Count == 0;
        }
        
    }
 
}

