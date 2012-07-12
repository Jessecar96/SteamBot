using System;
using System.Net;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using SteamKit2;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;

namespace SteamBot
{
	public class TradeSystem
	{
		public static String STEAM_COMMUNITY_DOMAIN = "steamcommunity.com";
		public static String STEAM_TRADE_URL = "http://steamcommunity.com/trade/{0}/";
		WebRequest webRequestStatus;
		string baseTradeURL;
		public SteamID meSID;
		public SteamID otherSID;
		public string steamLogin;
		public int version;
		public string sessionid;
		System.Threading.Timer Timer;
		public int logpos;
		
		public int TradeStatus;
		
		//Generic Trade info
		public bool OtherReady = false;
		public bool MeReady = false;
		
		//private
		private int NumEvents=0;
		private int NumLoops=0;
		
		//Inventories
		dynamic OtherItems;
		dynamic MyItems;
		
		//Cookies
		public CookieCollection WebCookies;
		
		
		static void printConsole(String line,ConsoleColor color = ConsoleColor.White)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(line);
            System.Console.ForegroundColor = ConsoleColor.White;
        }
		
		public TradeSystem (SteamID me, SteamID other, CookieCollection cookies)
		{
			this.meSID = me;
			this.otherSID = other;
			this.version = 1;
			this.logpos = 0;
			this.WebCookies = cookies;
			this.sessionid = cookies[0].Value;
			
			baseTradeURL = String.Format (STEAM_TRADE_URL, otherSID.ConvertToUInt64 ());
			
			//More Cookies
			cookies.Add(new Cookie("bCompletedTradeTutorial","true",null,STEAM_COMMUNITY_DOMAIN));
			cookies.Add(new Cookie("strTradeLastInventoryContext","440",null,STEAM_COMMUNITY_DOMAIN));
			cookies.Add(new Cookie("Steam_Language","english",null,STEAM_COMMUNITY_DOMAIN));
			cookies.Add(new Cookie("community_game_list_scroll_size","all",null,STEAM_COMMUNITY_DOMAIN));
			cookies.Add(new Cookie("strInventoryLastContext","99900_771150",null,STEAM_COMMUNITY_DOMAIN));
			cookies.Add(new Cookie("timezoneOffset","-14400,0",null,STEAM_COMMUNITY_DOMAIN));
			
			this.WebCookies = cookies;
		}
		
		public void initTrade ()
		{
			
			printConsole("[TradeSystem] Init Trade with "+otherSID,ConsoleColor.DarkGreen);
			
			//First Web Request
			try{
				webRequestStatus = CreateSteamRequest (baseTradeURL);
			
				HttpWebResponse response = webRequestStatus.GetResponse() as HttpWebResponse;
				StreamReader stream = new StreamReader(response.GetResponseStream());
			//string resp = stream.ReadToEnd();
			}catch(Exception){
				printConsole ("[TradeSystem][ERROR] Failed to connect to Steam!",ConsoleColor.Red);
			}
			
			//Get other player's inventory
			
			var request = CreateSteamRequest(baseTradeURL+"foreigninventory","POST");
			
			//POST Variables
			byte[] data = Encoding.ASCII.GetBytes("sessionid="+Uri.UnescapeDataString(sessionid)+"&steamid="+otherSID.ConvertToUInt64()+"&appid=440&contextid=2");
			
			//Headers
			request.ContentLength = data.Length;
			
			//Write it
			Stream poot = request.GetRequestStream();
			poot.Write(data,0,data.Length);
			
			HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
			Stream str = resp.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			string res = reader.ReadToEnd();
			
			//OtherInv = JsonConvert.DeserializeObject<UserInventory>(res);
			//rgItems[] = 
			
			var root = JsonConvert.DeserializeObject<JObject>(res);
			//rgItems[] OtherItems = root["rgInventory"].ToObject<rgItems[]>();
			
			Console.WriteLine("Inventory Status: "+root["success"]);
			//Console.WriteLine(root["rgInventory"]
			OtherItems = JObject.Parse(root["rgInventory"].ToString());
			
			//Debug
			
			/*
			foreach(dynamic i in OtherItems){
				
				Console.WriteLine("ITEM: "+i);
				
			}
			*/
			
			//Again
			
			//Console.WriteLine("[TradeSystem] Other User Items: "+OtherItems.Count());
			
			
			/*
			Console.WriteLine("[TradeSystem] First Response Cookies: "+response.Cookies.Count);
			for(int i=0;i<response.Cookies.Count;i++){
				Console.WriteLine("[TradeSystem][Cookies] Cookies["+i+"] = "+response.Cookies[i]);
			}
			*/
			
			sendChat ("Welcome to the Trade Bot!");
		
			
			//start refresh timer
			Timer = new System.Threading.Timer (TimerCallback, null, 0, 1000);
			
			
			
		}
		
		private void TimerCallback (object state)
		{
			//Timer.Dispose (); <-- ends loop
			
			
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
			
			//Console.WriteLine(res);
			
			
			StatusObj statusJSON = JsonConvert.DeserializeObject<StatusObj>(res);
			return statusJSON;
			
			
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
			
			//Events
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
					
					var person = (status.events[EventID].steamid==otherSID.ConvertToUInt64().ToString()) ? ("Them") : ("Me");
					
					//Print Statuses to console
					switch(status.events[EventID].action){
						
					case 0:
						Console.WriteLine("[TradeSystem]["+person+"] Added Item: "+status.events[EventID].assetid);
						break;
					case 1:
						Console.WriteLine("[TradeSystem]["+person+"] Removed Item: "+status.events[EventID].assetid);
						break;
					case 2:
						Console.WriteLine("[TradeSystem]["+person+"] Other User is ready.");
						break;
					case 3:
						Console.WriteLine("[TradeSystem]["+person+"] Other User is not ready.");
						break;
					case 7:
						Console.WriteLine("[TradeSystem]["+person+"] Chat: "+status.events[EventID].text);
						break;
					default:
						Console.WriteLine ("[TradeSystem]["+person+"] Unknown Event ID: " + status.events[EventID].action);
						break;
						
					}
				}
				
			}
			
			//Update Local Variables
			OtherReady = status.them.ready==1 ? true : false;
			MeReady = status.me.ready==1 ? true : false;
			
			//Console.WriteLine("Status: "+status.trade_status);
			
			if(status.trade_status==3){
				
				//Trade Cancelled
				Console.WriteLine("[TradeSystem] Trade Cancelled.");
				
				//End Timer
				Timer.Dispose ();
				
			}else if(status.trade_status==1){
				
				//Trade Complete
				Console.WriteLine("[TradeSystem] Trade Complete!");
				
				//End Timer
				Timer.Dispose ();
				
			}else if(status.trade_status==5){
				
				//Trade Failure
				Console.WriteLine("[TradeSystem] Trade Failed!");
				
				//End Timer
				Timer.Dispose ();
				
			}else if(status.trade_status==4){
				
				//Trade Timeout
				Console.WriteLine("[TradeSystem] Other user timed out.");
				
				//End Timer
				Timer.Dispose ();
				
			}else if(status.trade_status==0){
				
				//Just continue, this is normal.
				
			}
			
			//Update version
			if (status.newversion) {
				//Console.WriteLine ("[TradeSystem] Updated Version: " + status.version);
				//sendChat("Version Updated to "+version);
				this.version = status.version;
				this.logpos = status.logpos;
			}
			
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
		
		
		
		//Usefull
		private WebRequest CreateSteamRequest (string requestURL, string method = "GET")
		{
			HttpWebRequest webRequest = WebRequest.Create(requestURL) as HttpWebRequest;
            
            webRequest.Method = method;
			
			//webRequest
			
			//The Correct headers :D
			webRequest.Host = "steamcommunity.com";
			webRequest.Headers.Add("Origin","http://steamcommunity.com");
			webRequest.Headers.Add("X-Requested-With","XMLHttpRequest");
			webRequest.Headers.Add("X-Prototype-Version","1.7");
			webRequest.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";
			webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			webRequest.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
			webRequest.Referer = "http://steamcommunity.com/trade/1";
            webRequest.CookieContainer = new CookieContainer();
			webRequest.CookieContainer.Add(WebCookies);
			

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
		ChatMessage = 7
		
	}
}

