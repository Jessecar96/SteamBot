using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SteamKit2;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using System.Collections;

namespace SteamBot
{
	class MainClass : ICertificatePolicy
	{
		
		static SteamFriends steamFriends;
		public static SteamClient steamClient;
		static List<SteamID> clients = new List<SteamID>();
		public static CookieCollection WebCookies;
		
		public static string[] AllArgs;
		
		static TradeSystem trade;
		
		//Hacking around https
		public bool CheckValidationResult (ServicePoint sp, X509Certificate certificate, WebRequest request, int error)
		{
			return true;
		}
		
		
		static void printConsole(String line,ConsoleColor color = ConsoleColor.White, bool isDebug = false)
        {
			System.Console.ForegroundColor = color;
			if(isDebug){
				if(FindArg( AllArgs, "-debug" )){
					System.Console.WriteLine(line);
				}	
			}else{
				System.Console.WriteLine(line);
			}
            System.Console.ForegroundColor = ConsoleColor.White;
        }
		
		
		public static void Main (string[] args)
		{
			
			AllArgs = args;
			
			//Hacking around https
			ServicePointManager.CertificatePolicy = new MainClass ();
			
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine ("\n\tSteamBot Beta\n\tCreated by Jessecar96.\n\n");
			Console.ForegroundColor = ConsoleColor.White;
			
			
			steamClient = new SteamClient ();
			SteamTrading steamTrade = steamClient.GetHandler<SteamTrading>();
			SteamUser steamUser = steamClient.GetHandler<SteamUser> ();
			steamFriends = steamClient.GetHandler<SteamFriends>();
			
			steamClient.Connect ();
			
			
			while (true) {
				
				
				CallbackMsg msg = steamClient.WaitForCallback (true);
				
				printConsole (msg.ToString(),ConsoleColor.Blue,true);
				
				//Logged off
				msg.Handle<SteamUser.LoggedOffCallback> (callback =>
				{
					printConsole("Logged Off: "+callback.Result,ConsoleColor.Red);
				});
				
				
				//Disconnect from steam
				msg.Handle<SteamClient.DisconnectedCallback> (callback =>
				{
					printConsole("Disconnected.",ConsoleColor.Red);
				});
				
				
				//Steam Connected
				msg.Handle<SteamClient.ConnectedCallback> (callback =>
				{
					//Callback
					printConsole("Steam Connected Callback: "+callback.Result, ConsoleColor.Cyan);
					
					//Validate Result
					if(callback.Result==EResult.OK){
						
						
						
						//Steam Details
						
						printConsole("Username: ",ConsoleColor.Cyan);
						string user = Console.ReadLine();
						printConsole("Password: ",ConsoleColor.Cyan);
						Console.ForegroundColor = ConsoleColor.Black;
						string pass = Console.ReadLine();
						Console.ForegroundColor = ConsoleColor.White;
						
						
						//Console
						printConsole("Getting Web Cookies...",ConsoleColor.Yellow);
						
						//Web Cookies
						SteamWeb web = new SteamWeb();
						WebCookies = web.DoLogin (user,pass);
						
						if(WebCookies!=null){
							printConsole ("SteamWeb Cookies retrived.",ConsoleColor.Green);
							//Do Login
							steamUser.LogOn (new SteamUser.LogOnDetails{
								Username = user,
								Password = pass
							});
						}else{
							printConsole ("Error while getting SteamWeb Cookies.",ConsoleColor.Red);
						}
						
					}else{
						
						//Failure
						printConsole ("Failed to Connect to steam.",ConsoleColor.Red);	
					}
					
				});
				
				
				//Logged in (or not)
				msg.Handle<SteamUser.LoggedOnCallback>( callback =>
        		{
					printConsole("Logged on callback: "+callback.Result, ConsoleColor.Cyan);
					
					if(callback.Result != EResult.OK){
						printConsole("Login Failed!",ConsoleColor.Red);
					}else{
						printConsole("Successfulyl Logged In!\nWelcome "+steamUser.SteamID,ConsoleColor.Green);
						
						//Set community status
						steamFriends.SetPersonaName("TF2 TradeBOT Alpha");
						steamFriends.SetPersonaState(EPersonaState.Online);
					}
					
        		});
				
				//Trade Session Started
				msg.Handle<SteamTrading.TradeStartSessionCallback>(call =>
				{
					
					//Trading
					trade = new TradeSystem(steamUser.SteamID,call.Other,WebCookies);
					trade.initTrade();
					
				});
				
				//Trade Requested
				msg.Handle<SteamTrading.TradeProposedCallback>( thing =>
				{
					//Trade Callback
					printConsole ("Trade Proposed Callback. Other: "+thing.Other+"\n");
					
					//Accept It
					steamTrade.RequestTrade(thing.Other);
					
				});
				
				
				
				//Chat Callback
				msg.Handle<SteamFriends.FriendMsgCallback>(callback =>
                {
					//Type (emote or chat)
                    EChatEntryType type = callback.EntryType;
					
					if(type == EChatEntryType.ChatMsg){
						//Message is a chat message
						
						//Reply with the same message
						steamFriends.SendChatMessage(callback.Sender,EChatEntryType.ChatMsg,callback.Message);
						
						//Chat API coming soon
						
					}else if(type == EChatEntryType.Emote){
						//Message is emote
						
						//Do nothing yet
					}

                });
				
		
			} //end while loop
			
			
		} //end method
		
		public static int getIndex(SteamID sid)
        {
            for(int i=0;i<clients.Count();i++)
                if (clients[i] == sid)
                    return i;
            return -1;
        }
		
		static bool FindArg( string[] args, string arg )
        {
            foreach ( string potentialArg in args )
            {
                if ( potentialArg.IndexOf( arg, StringComparison.OrdinalIgnoreCase ) != -1 )
                    return true;
            }
            return false;
		}

		
	} //end class

		
} //end namespace

//Applejack is best Pony ;)