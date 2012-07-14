using System;
using System.Net;
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
		string baseTradeURL=null;
		public SteamID meSID;
		public SteamID otherSID;
		public string steamLogin;
		public int version;
		public string sessionid;
		Timer pollTimer;
		public int logpos;
		public int time;
		public int itemCount=0;
		
		public int TradeStatus;

		public List<String> ItemsList;
		
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
		rgInventory OtherAPIbp;
		itemSchema itemSchema;

		private bool problem;
		
		//Cookies
		public CookieCollection WebCookies;
		
		
		static void printConsole(String line,ConsoleColor color = ConsoleColor.White)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(line);
            System.Console.ForegroundColor = ConsoleColor.White;
        }
		
		public TradeSystem ()
		{

		}
		
		public void initTrade (SteamID me, SteamID other, CookieCollection cookies)
		{
			//Cleanup again
			cleanTrade ();

			//Setup
			ItemsList = new List<string>();
			meSID = me;
			otherSID = other;
			version = 1;
			logpos = 0;
			itemCount=0;
			time  = 0;
			WebCookies = cookies;
			sessionid = cookies [0].Value;
			
			baseTradeURL = String.Format (STEAM_TRADE_URL, otherSID.ConvertToUInt64 ());
			
			this.WebCookies = cookies;


			//Welcome...
			sendChat (String.Format ("Hello {0} Welcome to the RaffleBot! Wait just one sec, I'm loading your inventory.", MainClass.steamFriends.GetFriendPersonaName (otherSID)));

			
			printConsole ("[TradeSystem] Init Trade with " + otherSID, ConsoleColor.DarkGreen);
			

			try {

				//poll? poll.
				poll ();

			} catch (Exception) {
				printConsole ("[TradeSystem][ERROR] Failed to connect to Steam!", ConsoleColor.Red);
				//Send a message on steam
				MainClass.steamFriends.SendChatMessage (otherSID, EChatEntryType.ChatMsg, "Sorry, There was a problem connecting to Steam Trading.  Try again in a few minutes.");
				cleanTrade ();
			}



			

			printConsole ("[TradeSystem] Getting Player Inventories...", ConsoleColor.Yellow);

			int good = 0;

			OtherItems = getInventory (otherSID);
			if (OtherItems.success == "true") {
				printConsole ("[TradeSystem] Got Other player's inventory!", ConsoleColor.Green); 
				good++;
			}
				

			MyItems = getInventory (meSID);
			if (MyItems.success == "true") {
				printConsole ("[TradeSystem] Got the bot's inventory!", ConsoleColor.Green); 
				good++;
			}

			//Get Steam Inventory
			var request = CreateSteamRequest("http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=DF02BD82CF054DE26631BF1DEA9FCDE0&steamid="+otherSID.ConvertToUInt64());

			HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
			Stream str = resp.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			string res = reader.ReadToEnd();

			rgInventory json = JsonConvert.DeserializeObject<rgInventory>(res);
			OtherAPIbp = json;

			if (OtherAPIbp.result.status == "1") {
				printConsole("[TradeSystem] Loaded Other Backpack from API",ConsoleColor.Green);
				good++;
			}


			//get schema
			var schemaRequest = CreateSteamRequest("http://api.steampowered.com/IEconItems_440/GetSchema/v0001/?key=DF02BD82CF054DE26631BF1DEA9FCDE0");

			HttpWebResponse httpSchema = schemaRequest.GetResponse() as HttpWebResponse;
			Stream schemaStream = httpSchema.GetResponseStream();
			StreamReader readers = new StreamReader(schemaStream);
			string result = readers.ReadToEnd();

			//Console.WriteLine(result);

			itemSchema jsons = JsonConvert.DeserializeObject<itemSchema>(result);
			itemSchema = jsons;

			if (itemSchema.result.status == "1") {
				printConsole("[TradeSystem] Loaded Schema",ConsoleColor.Green);
				good++;
			}


			if (good == 4) {
				printConsole ("[TradeSystem] All is a go! Starting to Poll!", ConsoleColor.Green);

				//Timer
				pollTimer = new System.Threading.Timer (TimerCallback, null, 0, 1000);



				sendChat ("Done. Put up one or more hats to enter the raffle, and check the ready box when you're done.");
			} else {
				printConsole ("[TradeSystem][ERROR] status was not good! ABORT ABORT ABORT!", ConsoleColor.Red);
				MainClass.steamFriends.SendChatMessage(otherSID,EChatEntryType.ChatMsg,
				                                       "Sorry about this, but i'm having a problem getting one of our backpacks.  The Steam Community might be down.  Try again in a few minutes.");

			}


			/*
			 * How to Loop through the inventory:
			foreach(var child in OtherItems.rgInventory.Children())
			{
			    Console.WriteLine("Item ID: {0}", child.First.id);
			}
			*/
		}
		
		private void TimerCallback (object state)
		{
			//Refresh the Trade
			poll ();
			
		}
		
		private StatusObj getStatus ()
		{
			string res = null;
			
			//POST Variables
			byte[] data = Encoding.ASCII.GetBytes("sessionid="+Uri.UnescapeDataString(sessionid)+"&logpos="+logpos+"&version="+version);
			
			//Init
			var request = CreateSteamRequest(baseTradeURL+"tradestatus","POST");
			
			//Headers
			request.ContentLength = data.Length;
			
			//Write it
			Stream poot = request.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse response = request.GetResponse() as HttpWebResponse;
			Stream str = response.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			res = reader.ReadToEnd();
			
			
			StatusObj statusJSON = JsonConvert.DeserializeObject<StatusObj>(res);
			return statusJSON;
			
			
		}

		private dynamic getInventory (SteamID steamid)
		{

			var request = CreateSteamRequest(String.Format("http://steamcommunity.com/profiles/{0}/inventory/json/440/2/?trading=1",steamid.ConvertToUInt64()),"GET");
			
			HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
			Stream str = resp.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			string res = reader.ReadToEnd();

			dynamic json = JsonConvert.DeserializeObject(res);

			return json;

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

		public void poll ()
		{
			
			StatusObj status = getStatus ();
			
			try{
				if(NumEvents!=status.events.Length){
					
					NumLoops = status.events.Length-NumEvents;
					NumEvents = status.events.Length;
					
					for(int i=NumLoops;i>0;i--){
						
						int EventID;
						
						if(NumLoops==1){
							EventID = NumEvents-1;
						}else{
							EventID = NumEvents-i;
						}

						bool isBot = status.events[EventID].steamid!=otherSID.ConvertToUInt64().ToString();
						
						var person = (status.events[EventID].steamid==otherSID.ConvertToUInt64().ToString()) ? ("Them") : ("Me");
						
						//Print Statuses to console
						switch(status.events[EventID].action){
							
						case 0:
							Console.WriteLine("[TradeSystem]["+person+"] Added Item: "+status.events[EventID].assetid);
							ItemsList.Add(status.events[EventID].assetid.ToString());
							if(!isBot) time=0;
							break;
						case 1:
							Console.WriteLine("[TradeSystem]["+person+"] Removed Item: "+status.events[EventID].assetid);
							ItemsList.Remove(status.events[EventID].assetid.ToString());
							if(!isBot) time=0;
							break;
						case 2:
							Console.WriteLine("[TradeSystem]["+person+"] set ready.");
							if(!isBot){
								problem=false;
								itemCount=0;
								Console.WriteLine("[TradeSystem] Validating Items...");
								sendChat("Just hold on a sec.  I gotta check these items to make sure they're eligable.  Don't touch the trade.");
								if(!isBot) time=0;

								foreach(rgItems item in OtherAPIbp.result.items){

									if(ItemsList.Contains(item.id.ToString())){

										foreach(itItems schemaItem in itemSchema.result.items){
											//Console.WriteLine(schemaItem.defindex+" - "+schemaItem.craft_material_type+" - "+schemaItem.item_type_name);
											string type = schemaItem.craft_material_type;
											if(item.defindex==schemaItem.defindex){
												printConsole("Item found in the schema! Type: "+type,ConsoleColor.Cyan);
												if(type=="hat"){
													itemCount++;
												}else{
													sendChat (String.Format ("The item {0} is not eligable for the raffle, please remove it from the trade.",schemaItem.name));
													problem=true;
												}
											}
										}

									}

								}

								if(problem){
									//sendChat ("There are problems in your trade.  Please fix the problems that were listed above.");
									setReady(false);
									problem=false;
								}else{
									sendChat("You're all set to go! Your item's are verified.");
									setReady(true);
								}


								//setReady(true);
							}
							break;
						case 3:
							Console.WriteLine("[TradeSystem]["+person+"] set not ready.");
							if(!isBot){
								Console.WriteLine("[TradeSystem] Refusing Trade.");
								setReady(false);
							}
							if(!isBot) time=0;
							break;
						case 4:
							Console.WriteLine("[TradeSystem]["+person+"] Accepting");
							Console.WriteLine("[TradeSystem] Sending update...",ConsoleColor.Cyan);
							string th = sendUpdate();
							Console.WriteLine(th);
							if(!isBot){
								//Accept It
								dynamic js = acceptTrade();
								if(js.success==true){
									printConsole("[TradeSystem] Trade was successfull! Resetting System...",ConsoleColor.Green);
									MainClass.steamFriends.SendChatMessage(otherSID,EChatEntryType.ChatMsg,
									                                       "Thank you for entering the raffle!  Keep watch on the raffle page to know when it's time to claim your prize.");
									MainClass.steamFriends.SendChatMessage(otherSID,EChatEntryType.ChatMsg,
									                                       "Have a nice day!");
								}else{
									printConsole("[TradeSystem] Poo! Trade might of Failed :C Well, resetting anyway...",ConsoleColor.Red);
								}
								cleanTrade();
							}
							break;
						case 7:
							Console.WriteLine("[TradeSystem]["+person+"] Chat: "+status.events[EventID].text);
							if(!isBot){
								if(status.events[EventID].text=="/dump"){
									//doAdump();

								}
							}
							break;
						default:
							Console.WriteLine ("[TradeSystem]["+person+"] Unknown Event ID: " + status.events[EventID].action);
							break;


							
						}
					}
					
				}else{

					time++;
					if(time>55){
						cleanTrade();
						MainClass.steamFriends.SendChatMessage(otherSID,EChatEntryType.ChatMsg,
									                                       "Sorry, but you were AKF and the trade was canceled.");
						printConsole ("[TradeSystem] User was kicked because he was AFK.",ConsoleColor.Red);
					}else if(time>35){
						sendChat ("Are You AFK? The trade will be canceled in "+(55-time)+" seconds if you don't do something.");
					}

				}
			}catch(Exception x){

				printConsole(x.ToString(),ConsoleColor.DarkYellow);
				exc++;
				printConsole("Exception while getting status.  Count: "+exc,ConsoleColor.DarkYellow);
				if(exc==5){
					MainClass.steamFriends.SendChatMessage(otherSID,EChatEntryType.ChatMsg,
									                                       "Sorry, but the trade timed out.  Try again.");
					printConsole("[TradeSystem] 5th Exception, Disconnecting.",ConsoleColor.Red);
					cleanTrade();
					exc=0;
				}
				return;

			}
			
			//Update Local Variables
			OtherReady = status.them.ready==1 ? true : false;
			MeReady = status.me.ready==1 ? true : false;

			
			//Update version
			if (status.newversion) {
				this.version = status.version;
				this.logpos = status.logpos;
			}
			
		}

		public void doAdump()
		{

			int slot = 0;

			foreach(var child in MyItems.rgInventory.Children())
			{
				printConsole ("[TradeSystem][DUMP] Adding Item ID "+child.First.id+" to slot "+slot,ConsoleColor.Cyan);
				addItem(child.First.id.ToString(),slot);
				poll ();
				Thread.Sleep (200);
				slot++;
			}

			printConsole ("[TradeSystem][DUMP] Item Dump Finished.",ConsoleColor.Cyan);
			sendChat("Done Dumping.");


		}

		public void cleanTrade ()
		{
			//Cleanup!
			try {
				//End Polling
				pollTimer.Dispose ();
				this.ItemsList.Clear();
			} catch (Exception) {
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
			this.pollTimer = null;
			this.sessionid = null;
			this.steamLogin = null;
			this.TradeStatus = 0;
			this.version = 0;
			this.WebCookies = null;



		}
		
		public string sendChat(string msg){
			/*
			 * 
			 *  sessionid: g_sessionID,
		 	 *	message: strMessage,
		 	 *	logpos: g_iNextLogPos,
			 *	version: g_rgCurrentTradeStatus.version
			 * 
			 */
			string res=null;
			
			byte[] data = Encoding.ASCII.GetBytes("sessionid="+Uri.UnescapeDataString(sessionid)+"&message="+Uri.EscapeDataString(msg)+"&logpos="+Uri.EscapeDataString(""+logpos)+"&version="+Uri.EscapeDataString(""+version));
			
			
			var req = CreateSteamRequest(baseTradeURL+"chat","POST");
			
			
			req.ContentLength = data.Length;
			
			Stream poot = req.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse response = req.GetResponse() as HttpWebResponse;
			Stream str = response.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			res = reader.ReadToEnd();
			
			return res;
			
		}

		public string sendUpdate(){

			string res=null;

			List<updItem> finalList = new List<updItem>();

			foreach(rgItems item in OtherAPIbp.result.items){
				if(ItemsList.Contains(item.id.ToString())){
					foreach(itItems schemaItem in itemSchema.result.items){
						if(item.defindex==schemaItem.defindex){
							finalList.Add(new updItem{id=item.id.ToString(),defindex=schemaItem.defindex});
						}
					}
				}
			}

			JavaScriptSerializer serializer = new JavaScriptSerializer();

			string items = serializer.Serialize(finalList);

			
			byte[] data = Encoding.ASCII.GetBytes(String.Format ("do=insert&steamid={0}&count={1}&items={2}",Uri.EscapeUriString(otherSID.ConvertToUInt64().ToString()),Uri.EscapeUriString(""+itemCount),items));
			
			
			var req = CreateSteamRequest("http://jessecar96.net/raffle/update.php","POST");

			
			req.ContentLength = data.Length;
			
			Stream poot = req.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse response = req.GetResponse() as HttpWebResponse;
			Stream str = response.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			res = reader.ReadToEnd();
			
			return res;
			
		}


		public dynamic acceptTrade ()
		{

			//toggleready
			string res=null;

			byte[] data = Encoding.ASCII.GetBytes("sessionid="+Uri.UnescapeDataString(sessionid)+"&version="+Uri.EscapeDataString(""+version));

			var req = CreateSteamRequest(baseTradeURL+"confirm","POST");
			
			req.ContentLength = data.Length;

			Stream poot = req.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse response = req.GetResponse() as HttpWebResponse;
			Stream str = response.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			res = reader.ReadToEnd();

			dynamic json = JsonConvert.DeserializeObject(res);
			return json;

		}


		public void addItem (string itemid, int slot)
		{
			//toggleready
			string res=null;

			byte[] data = Encoding.ASCII.GetBytes(String.Format("sessionid={0}&appid=440&contextid=2&itemid={1}&slot={2}",Uri.UnescapeDataString(sessionid),itemid,slot));

			var req = CreateSteamRequest(baseTradeURL+"additem","POST");
			
			req.ContentLength = data.Length;
			
			Stream poot = req.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse response = req.GetResponse() as HttpWebResponse;
			Stream str = response.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			res = reader.ReadToEnd();


		}


		public void setReady (bool ready)
		{
			//toggleready
			string res=null;

			string red = ready ? "true" : "false";

			byte[] data = Encoding.ASCII.GetBytes("sessionid="+Uri.UnescapeDataString(sessionid)+"&ready="+Uri.EscapeDataString(red)+"&version="+Uri.EscapeDataString(""+version));

			var req = CreateSteamRequest(baseTradeURL+"toggleready","POST");
			
			req.ContentLength = data.Length;
			
			Stream poot = req.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse response = req.GetResponse() as HttpWebResponse;
			Stream str = response.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			res = reader.ReadToEnd();

		}
		
		
		
		//Usefull
		private WebRequest CreateSteamRequest (string requestURL, string method = "GET")
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

			webRequest.Headers.Add("Origin","http://steamcommunity.com");
			webRequest.Headers.Add("X-Requested-With","XMLHttpRequest");
			webRequest.Headers.Add("X-Prototype-Version","1.7");

			CookieContainer cookies = new CookieContainer();
			cookies.Add (new Cookie("sessionid",WebCookies["sessionid"].Value,String.Empty,STEAM_COMMUNITY_DOMAIN));
			cookies.Add (new Cookie("steamLogin",WebCookies["steamLogin"].Value,String.Empty,STEAM_COMMUNITY_DOMAIN));

			webRequest.CookieContainer = cookies;
			

            return webRequest;
		}
	}
	
	
	
	
	public class StatusObj
	{
		
		public string error {get;set;}
		
		public bool newversion { get; set; }
		
		public bool success { get; set; }
		
		public long trade_status{ get; set; }
		
		public int version{ get; set; }
		
		public int logpos{get;set;}
		
		public TradeUserObj me{get;set;}
		
		public TradeUserObj them{get;set;}
		
		public TradeEvents[] events{get;set;}
		
	}
	
	public class TradeEvents
	{
		public string steamid{get;set;}
		
		public int action{get;set;}
		
		public long timestamp{get;set;}
		
		public int appid{get;set;}
		
		public string text{get;set;}
		
		public int contextid{get;set;}
		
		public long assetid{get;set;}
			
	}
	
	public class TradeUserObj
	{
		
		public int ready{get;set;}
		
		public int confirmed{get;set;}
		
		public int sec_since_touch{get;set;}
		
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
		public string defindex{ get; set; }

		public string id{get;set;}

	}
}

