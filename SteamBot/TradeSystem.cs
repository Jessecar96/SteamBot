using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SteamKit2;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Web.Script.Serialization;

namespace SteamBot
{
    public class TradeSystem
    {
        public static String STEAM_COMMUNITY_DOMAIN = "steamcommunity.com";
        public static String STEAM_TRADE_URL = "http://steamcommunity.com/trade/{0}/";
        string baseTradeURL = null;
        public SteamID meSID;
        public SteamID otherSID;
        public string steamLogin;
        public int version;

        public Bot theBot;

        public string sessionid
        {
            get { return theBot.sessionId; }
        }

        public bool isAdmin
        {
            get { return theBot.Admins.Contains(otherSID.ConvertToUInt64()); }
        }

        public int logpos;
        public int time;
        public int totalTime;
        public int itemCount;

        public int validateTimer = 0;

        public bool otherReady, isConfirmed;

        public int TradeStatus;

        public int ScrapSent;

        public List<String> ItemsList;

        public List<InventoryItem> ItemsPutUp = new List<InventoryItem>();

        public List<ulong> OtherOfferedItems = new List<ulong>();
        public List<ulong> MyOfferedItems = new List<ulong>();

        //Generic Trade info
        public bool OtherReady = false;
        public bool MeReady = false;

        //private
        private int NumEvents;
        private int NumLoops;
        private int exc;

        //Inventories
        dynamic OtherItems;
        dynamic MyItems;
        protected PlayerInventory OtherAPIbp;
        protected PlayerInventory MyAPIbp;
        public static ItemSchema itemSchema = null;

        private bool problem;

        public static void GetSchema()
        {
            if (itemSchema != null) return;
            //get schema
            var schemaRequest = CreateSteamRequest("http://api.steampowered.com/IEconItems_440/GetSchema/v0001/?key=WHOOPS");

            HttpWebResponse httpSchema = schemaRequest.GetResponse() as HttpWebResponse;
            Stream schemaStream = httpSchema.GetResponseStream();
            StreamReader readers = new StreamReader(schemaStream);
            string result = readers.ReadToEnd();

            //Console.WriteLine(result);

            ItemSchema jsons = JsonConvert.DeserializeObject<ItemSchema>(result);
            itemSchema = jsons;
        }

        protected static void printConsole(String line, ConsoleColor color = ConsoleColor.White)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(line);
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        public TradeSystem()
        {

        }

        public void AddAllOf(int num, int defindex)
        {
            for (int i = 0; i < num; i++)
            {
                foreach (InventoryItem item in MyAPIbp.result.items)
                {
                    if (ItemsPutUp.Contains(item)) continue;
                    if (item.defindex == defindex.ToString())
                    {
                        //SCRAP! YAY!
                        addItem(item.id, i);
                        ItemsPutUp.Add(item);
                        break;
                    }
                }
            }
        }

        public virtual void OnInit()
        {

        }

        public virtual void OnSuccessfulInit()
        {

        }

        public bool initTrade(Bot bot, SteamID me, SteamID other, CookieCollection cookies)
        {
            theBot = bot;
            //Cleanup again
            cleanTrade();

            //Setup
            ItemsList = new List<string>();
            meSID = me;
            otherSID = other;
            version = 1;
            logpos = 0;
            itemCount = 0;
            time = 0;

            baseTradeURL = String.Format(STEAM_TRADE_URL, otherSID.ConvertToUInt64());

            OnInit();


            //Welcome...
            sendChat(String.Format("Hello {0}. Welcome to the raffle bot! Please wait while I load the inventories.", theBot.steamFriends.GetFriendPersonaName(otherSID)));


            printConsole("Initializing Trade System...", ConsoleColor.Cyan);


            try
            {

                //poll? poll.
                poll();

            }
            catch (Exception)
            {
                printConsole("Failed to connect to Steam!", ConsoleColor.Red);
                //Send a message on steam
                try
                {
                    theBot.steamFriends.SendChatMessage(otherSID, EChatEntryType.ChatMsg, "Sorry, There was a problem connecting to Steam Trading.  Try again in a few minutes.");
                    cleanTrade();
                }
                catch (Exception) { }
            }

            printConsole("Getting Player Inventories...", ConsoleColor.Cyan);

            int good = 0;

            OtherItems = getInventory(otherSID);
            if (OtherItems != null && OtherItems.success == "true")
            {
                printConsole("Got Other player's inventory!", ConsoleColor.Cyan);
                good++;
            }


            MyItems = getInventory(meSID);
            if (MyItems != null && MyItems.success == "true")
            {
                printConsole("Got the bot's inventory from trading API!", ConsoleColor.Cyan);
                good++;
            }

            OtherAPIbp = getRealInventory(otherSID);
            if (OtherAPIbp.result != null && OtherAPIbp.result.status == "1")
            {
                printConsole("Loaded Other Backpack from API", ConsoleColor.Cyan);
                good++;
            }

            MyAPIbp = getRealInventory(meSID);
            if (itemSchema.result != null && itemSchema.result.status == "1")
            {
                printConsole("Loaded Schema.", ConsoleColor.Cyan);
                good++;
            }


            if (good == 4)
            {
                printConsole("We are good to go! Polling...", ConsoleColor.Cyan);

                OnSuccessfulInit();
                return true;
            }
            else
            {
                printConsole("Oh boy! There was a problem getting something :((", ConsoleColor.Red);
                try
                {
                    theBot.steamFriends.SendChatMessage(otherSID, EChatEntryType.ChatMsg,
                                                           "Sorry about this, but I'm having a problem getting one of our backpacks. The Steam Community might be down. Ensure your backpack isn't private. Try again in a few minutes.");
                }
                catch (Exception e)
                {

                }
                return false;
            }
        }

        protected void SetName(string name)
        {
            theBot.steamFriends.SetPersonaName(name);
        }

        private StatusObj getStatus()
        {
            try
            {
                string res = null;

                //POST Variables
                byte[] data = Encoding.ASCII.GetBytes("sessionid=" + Uri.UnescapeDataString(sessionid) + "&logpos=" + logpos + "&version=" + version);

                //Init
                var request = CreateSteamRequest(baseTradeURL + "tradestatus", theBot, "POST");

                //Headers
                request.ContentLength = data.Length;

                //Write it
                Stream poot = request.GetRequestStream();
                poot.Write(data, 0, data.Length);

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream str = response.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                res = reader.ReadToEnd();


                StatusObj statusJSON = JsonConvert.DeserializeObject<StatusObj>(res);
                return statusJSON;
            }
            catch (Exception i)
            {
            }
            return null;
        }

        public PlayerInventory getRealInventory(SteamID steamid)
        {
            try
            {
                var request = CreateSteamRequest("http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=DF02BD82CF054DE26631BF1DEA9FCDE0&steamid=" + steamid.ConvertToUInt64(), theBot, "GET", false);
                HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                Stream str = resp.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                string res = reader.ReadToEnd();

                return JsonConvert.DeserializeObject<PlayerInventory>(res);
            }
            catch (NullReferenceException c)
            {
                printConsole("Problem! Null Refrence Exception!!!", ConsoleColor.Red);
                return new PlayerInventory();
            }
            catch (WebException x)
            {
                printConsole("Problem! Steam API returned: " + x.Status, ConsoleColor.Red);
                return new PlayerInventory();
            }
        }

        protected dynamic getInventory(SteamID steamid)
        {
            try
            {
                var request =
                    CreateSteamRequest(
                        String.Format("http://steamcommunity.com/profiles/{0}/inventory/json/440/2/?trading=1",
                                      steamid.ConvertToUInt64()), theBot, "GET");

                HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                Stream str = resp.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                string res = reader.ReadToEnd();

                dynamic json = JsonConvert.DeserializeObject(res);

                return json;
            }
            catch (Exception e)
            {
                return JsonConvert.DeserializeObject("{\"success\":\"false\"}");
            }
        }





        public virtual void OnUserAddItem(SchemaItem schemaItem, InventoryItem invItem)
        {
            printConsole("User added item:" + schemaItem.item_name, ConsoleColor.Cyan);
        }

        public virtual void OnUserRemoveItem(SchemaItem schemaItem, InventoryItem invItem)
        {

        }

        public virtual void OnUserSetReady()
        {

        }

        public virtual void OnUserSetUnready()
        {

        }

        public virtual void OnUserAccept()
        {

        }

        public virtual void OnTradeEnd(bool success)
        {
        }


        public virtual void OnUserChat(string message)
        {

        }

        public virtual void OnValidate()
        {

        }


        /**
         * 
         * Trade Action ID's
         * 
         * 0 = Add item (itemid = "assetid")
         * 1 = remove item (itemid = "assetid")
         * 2 = Toggle ready
         * 3 = Toggle not ready
         * 4
         * 5
         * 6
         * 7 = Chat (message = "text")
         * 
         */

        public void poll()
        {
            StatusObj status = getStatus();

            try
            {
                if (NumEvents != status.events.Length)
                {
                    NumLoops = status.events.Length - NumEvents;
                    NumEvents = status.events.Length;

                    for (int i = NumLoops; i > 0; i--)
                    {

                        int EventID;

                        if (NumLoops == 1)
                        {
                            EventID = NumEvents - 1;
                        }
                        else
                        {
                            EventID = NumEvents - i;
                        }

                        bool isBot = status.events[EventID].steamid != otherSID.ConvertToUInt64().ToString();

                        var person = (status.events[EventID].steamid == otherSID.ConvertToUInt64().ToString()) ? (theBot.steamFriends.GetFriendPersonaName(otherSID)) : ("Me");

                        //Print Statuses to console
                        ulong itemID;
                        switch (status.events[EventID].action)
                        {
                            case 0:
                                itemID = (ulong)status.events[EventID].assetid;
                                ItemsList.Add(itemID.ToString());

                                if (isBot)
                                    MyOfferedItems.Add(itemID);
                                else
                                {
                                    OtherOfferedItems.Add(itemID);
                                    InventoryItem item = OtherAPIbp.GetItem(itemID);
                                    SchemaItem SchemaItem = item.GetSchemaItem(itemSchema);
                                    OnUserAddItem(SchemaItem, item);
                                }
                                if (!isBot) time = 0;
                                if (isBot) break;
                                validateTimer = 2;
                                break;
                            case 1:
                                itemID = (ulong)status.events[EventID].assetid;
                                if (isBot)
                                    MyOfferedItems.Remove(itemID);
                                else
                                {
                                    OtherOfferedItems.Remove(itemID);
                                    InventoryItem item = OtherAPIbp.GetItem(itemID);
                                    SchemaItem SchemaItem = item.GetSchemaItem(itemSchema);
                                    OnUserRemoveItem(SchemaItem, item);
                                }
                                ItemsList.Remove(itemID.ToString());
                                if (!isBot) time = 0;
                                if (isBot) break;
                                break;
                            case 2:
                                otherReady = true;
                                printConsole("User set ready.", ConsoleColor.Cyan);
                                if (isBot) break;

                                OnUserSetReady();


                                break;
                            case 3:
                                otherReady = false;
                                printConsole("User set not ready.", ConsoleColor.Cyan);
                                if (!isBot)
                                {
                                    // Console.WriteLine("Refusing Trade.");
                                    OnUserSetUnready();
                                }
                                if (!isBot) time = 0;
                                break;
                            case 4:
                                if (!isBot)
                                {
                                    printConsole("User Accepting", ConsoleColor.Cyan);
                                    OnUserAccept();
                                }
                                break;
                            case 7:
                                // Console.WriteLine("[TradeSystem][" + person + "] Chat: " + );
                                if (!isBot)
                                {
                                    printConsole("User Chat: " + status.events[EventID].text, ConsoleColor.Cyan);
                                    string message = status.events[EventID].text;
                                    OnUserChat(message);


                                    if (!isAdmin) break;

                                    if (message.Contains("/givespecific"))
                                    {
                                        Regex r = new Regex("\\d+");
                                        if (!r.IsMatch(message)) break;
                                        Match m = r.Match(message);
                                        string index = m.Value;
                                        addItemDefIndex(index, 0);
                                    }

                                    if (message.Contains("/giverec"))
                                    {
                                        Regex r = new Regex("\\d+");
                                        if (!r.IsMatch(message)) break;
                                        Match m = r.Match(message);
                                        string index = m.Value;
                                        AddAllOf(Convert.ToInt32(index), 5001);
                                    }

                                    if (message.Contains("/givescrap"))
                                    {
                                        Regex r = new Regex("\\d+");
                                        if (!r.IsMatch(message)) break;
                                        Match m = r.Match(message);
                                        string index = m.Value;
                                        AddAllOf(Convert.ToInt32(index), 5000);
                                    }

                                    if (message.Contains("/giveref"))
                                    {
                                        Regex r = new Regex("\\d+");
                                        if (!r.IsMatch(message)) break;
                                        Match m = r.Match(message);
                                        string index = m.Value;
                                        AddAllOf(Convert.ToInt32(index), 5002);
                                    }
                                }
                                break;
                            default:
                                printConsole("Unknown Event ID: " + status.events[EventID].action, ConsoleColor.Red);
                                break;
                        }
                    }

                }
                else
                {
                    time++;
                    totalTime++;
                    if (time > 30 || totalTime > 180)
                    {
                        theBot.steamFriends.SendChatMessage(otherSID, EChatEntryType.ChatMsg,
                                                                           "Sorry, but you were AFK and the trade was canceled.");
                        printConsole("User was kicked because he was AFK.", ConsoleColor.Cyan);
  
                    }
                    else if (time > 15 && time % 5 == 0)
                    {
                        sendChat("Are You AFK? The trade will be canceled in " + (30 - time) + " seconds if you don't do something.");
                    }
                    if (validateTimer > 0)
                    {
                        if (--validateTimer <= 0)
                        {
                            validateTimer = 0;
                            OnValidate();
                        }
                    }
                }
            }
            catch (Exception x)
            {
                return;
            }

            //Update Local Variables
            OtherReady = status.them.ready == 1 ? true : false;
            MeReady = status.me.ready == 1 ? true : false;


            //Update version
            if (status.newversion)
            {
                this.version = status.version;
                this.logpos = status.logpos;
            }

        }

        public virtual void OnCleanTrade()
        {

        }

        public void cleanTrade()
        {
            //Cleanup!
            try
            {
                this.ItemsList.Clear();
                this.ItemsPutUp.Clear();
            }
            catch (Exception)
            {
            }

            //Clean ALL THE VARIABLES

            this.ItemsList = null;
            this.baseTradeURL = null;
            this.exc = 0;
            this.logpos = 0;
            this.MeReady = false;
            this.meSID = null;
            this.MyItems = null;
            this.NumEvents = 0;
            this.NumLoops = 0;
            this.OtherItems = null;
            this.OtherReady = false;
            this.otherSID = null;
            this.steamLogin = null;
            this.TradeStatus = 0;
            this.version = 0;
            OnCleanTrade();



        }

        public string sendChat(string msg)
        {
            /*
             * 
             *  sessionid: g_sessionID,
             *	message: strMessage,
             *	logpos: g_iNextLogPos,
             *	version: g_rgCurrentTradeStatus.version
             * 
             */
            try
            {
                string res = null;

                byte[] data =
                    Encoding.ASCII.GetBytes("sessionid=" + Uri.UnescapeDataString(sessionid) + "&message=" +
                                            Uri.EscapeDataString(msg) + "&logpos=" + Uri.EscapeDataString("" + logpos) +
                                            "&version=" + Uri.EscapeDataString("" + version));


                var req = CreateSteamRequest(baseTradeURL + "chat", theBot, "POST");


                req.ContentLength = data.Length;

                Stream poot = req.GetRequestStream();
                poot.Write(data, 0, data.Length);

                HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                Stream str = response.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                res = reader.ReadToEnd();

                return res;
            }
            catch (Exception e)
            {
                return "";
            }

        }

        public dynamic acceptTrade()
        {
            try
            {
                //toggleready
                string res = null;

                byte[] data =
                    Encoding.ASCII.GetBytes("sessionid=" + Uri.UnescapeDataString(sessionid) + "&version=" +
                                            Uri.EscapeDataString("" + version));

                var req = CreateSteamRequest(baseTradeURL + "confirm", theBot, "POST");

                req.ContentLength = data.Length;

                Stream poot = req.GetRequestStream();
                poot.Write(data, 0, data.Length);

                HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                Stream str = response.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                res = reader.ReadToEnd();

                dynamic json = JsonConvert.DeserializeObject(res);
                return json;
            }
            catch (Exception e)
            {
                return null;
            }

        }

        public void addItemDefIndex(string defindex, int slot)
        {
            foreach (InventoryItem item in MyAPIbp.result.items)
            {
                if (ItemsList.Contains(item.id)) continue;
                foreach (SchemaItem schemaItem in itemSchema.result.items)
                {
                    if (item.defindex == schemaItem.defindex && item.defindex == defindex)
                    {
                        addItem(item.id, slot);
                        return;
                    }
                }
            }
        }

        public void addItem(string itemid, int slot)
        {
            try
            {
                //toggleready
                string res = null;

                byte[] data =
                    Encoding.ASCII.GetBytes(String.Format("sessionid={0}&appid=440&contextid=2&itemid={1}&slot={2}",
                                                          Uri.UnescapeDataString(sessionid), itemid, slot));

                var req = CreateSteamRequest(baseTradeURL + "additem", theBot, "POST");

                req.ContentLength = data.Length;

                Stream poot = req.GetRequestStream();
                poot.Write(data, 0, data.Length);

                HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                Stream str = response.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                res = reader.ReadToEnd();
            }
            catch
            {

            }
        }

        public void rmItem(string itemid, int slot)
        {
            try
            {
                //toggleready
                string res = null;

                byte[] data =
                    Encoding.ASCII.GetBytes(String.Format("sessionid={0}&appid=440&contextid=2&itemid={1}&slot={2}",
                                                          Uri.UnescapeDataString(sessionid), itemid, slot));

                var req = CreateSteamRequest(baseTradeURL + "removeitem", theBot, "POST");

                req.ContentLength = data.Length;

                Stream poot = req.GetRequestStream();
                poot.Write(data, 0, data.Length);

                HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                Stream str = response.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                res = reader.ReadToEnd();
            }
            catch { }
        }


        public void setReady(bool ready)
        {
            try
            {
                isConfirmed = ready;
                //toggleready
                string res = null;

                string red = ready ? "true" : "false";

                byte[] data =
                    Encoding.ASCII.GetBytes("sessionid=" + Uri.UnescapeDataString(sessionid) + "&ready=" +
                                            Uri.EscapeDataString(red) + "&version=" + Uri.EscapeDataString("" + version));

                var req = CreateSteamRequest(baseTradeURL + "toggleready", theBot, "POST");

                req.ContentLength = data.Length;

                Stream poot = req.GetRequestStream();
                poot.Write(data, 0, data.Length);

                HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                Stream str = response.GetResponseStream();
                StreamReader reader = new StreamReader(str);
                res = reader.ReadToEnd();
            }
            catch
            {

            }

        }

        //Usefull
        public static WebRequest CreateSteamRequest(string requestURL, Bot bot = null, string method = "GET", bool cookiesDo = true)
        {
            HttpWebRequest webRequest = WebRequest.Create(requestURL) as HttpWebRequest;

            webRequest.Method = method;

            //webRequest

            //The Correct headers :D
            webRequest.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            webRequest.Host = "steamcommunity.com";
            webRequest.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
            webRequest.Referer = "http://steamcommunity.com/trade/1";

            webRequest.Headers.Add("Origin", "http://steamcommunity.com");
            webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
            webRequest.Headers.Add("X-Prototype-Version", "1.7");

            CookieContainer cookies = new CookieContainer();
            if (bot != null)
            {
                cookies.Add(new Cookie("sessionid", bot.sessionId, String.Empty, STEAM_COMMUNITY_DOMAIN));
                cookies.Add(new Cookie("steamLogin", bot.token, String.Empty, STEAM_COMMUNITY_DOMAIN));
            }

            webRequest.CookieContainer = cookies;


            return webRequest;
        }
    }




    public class StatusObj
    {

        public string error { get; set; }

        public bool newversion { get; set; }

        public bool success { get; set; }

        public long trade_status { get; set; }

        public int version { get; set; }

        public int logpos { get; set; }

        public TradeUserObj me { get; set; }

        public TradeUserObj them { get; set; }

        public TradeEvents[] events { get; set; }

    }

    public class TradeEvents
    {
        public string steamid { get; set; }

        public int action { get; set; }

        public long timestamp { get; set; }

        public int appid { get; set; }

        public string text { get; set; }

        public int contextid { get; set; }

        public long assetid { get; set; }

    }

    public class TradeUserObj
    {

        public int ready { get; set; }

        public int confirmed { get; set; }

        public int sec_since_touch { get; set; }

    }

    public enum ETradeEvents
    {

        AddItem = 0,
        RemoveItem = 1,
        ToggleReady = 2,
        ToggeNotReady = 3,
        OtherUserAccept = 4,
        ChatMessage = 7

    }

    public class updItem
    {
        public string defindex { get; set; }

        public string id { get; set; }

    }
}

