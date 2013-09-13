using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using System;
using System.Timers;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace SteamBot
{
    public class StockHandler : UserHandler
    {



        public StockHandler(Bot bot, SteamID sid)
            : base(bot, sid) 
        {
        }
        public override void OnTradeSuccess()
        {

        }
        public override bool OnFriendAdd () 
        {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") added me!");
            // Using a timer here because the message will fail to send if you do it too quickly
            
            return true;
        }
        public void ReInit()
        {

        }


        public override void OnLoginCompleted()
        {
            const string UrlBase = "http://store.valvesoftware.com/index.php?t=2&g=10";
            string url = UrlBase;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url) ;
            request.Method = "GET";
            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.Host = "store.valvesoftware.com";
            request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
            request.Referer = "http://store.valvesoftware.com";
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            var reader = new StreamReader(response.GetResponseStream());
            string result = reader.ReadToEnd();
            response.Close();
            Regex r = new Regex("product.php");
            int x = r.Matches(result).Count;
            if (x > 14)
            {
                Log.Warn("valve商店有新物品" + "\r\n");
                Log.Warn(x.ToString());
            }
            else
            {
                Log.Warn("valve商店没有新物品" + "\r\n");
                Log.Warn( x.ToString ());
            }
        }

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {

        }

        public override void OnFriendRemove () 
        {

        }
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
           

        }

        public override bool OnTradeRequest() 
        {
            return true;

        }
        
        public override void OnTradeError (string error) 
        {

        }
        
        public override void OnTradeTimeout () 
        {

        }
        
        public override void OnTradeInit() 
        {
            ReInit();

        }
        
        
          
        public override void OnTradeAddItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
        }
   
        public override void OnTradeRemoveItem (Schema.Item schemaItem, Inventory.Item inventoryItem) 
        {
            

        }
        
         public override void OnTradeMessage(string message) //根据用户在交易窗口的指令添加及移除卡
        {

        }
        
        public override void OnTradeReady (bool ready) 
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else 
            {
                Bot.log.Success("User is ready to trade!");
                if (Validate())
                {
                    Trade.SetReady(true);
                }
                else
                {
                    
                    Trade.SetReady(false);
                }

            }
           
                    
               
         
        }
        
        public override void OnTradeAccept() 
        {

            if (Validate())
            {
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
            else
            {
                Trade.SetReady(false);
            }
            OnTradeClose ();
        }
        public override void OnTradeClose()
        {
            Bot.SteamFriends.SetPersonaState(EPersonaState.Online);
            //Bot.log.Warn("[USERHANDLER] TRADE CLOSED");
            base.OnTradeClose();
        }

        public bool Validate ()
        {

                return true;

        }
        
    }
 
}

