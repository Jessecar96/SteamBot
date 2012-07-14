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
		
		//SteamRE Variables
		public static SteamFriends steamFriends;
		public static SteamClient steamClient;
		public static SteamTrading steamTrade;
		
		//Trading Variables
		public static CookieCollection WebCookies;
		public static TradeSystem trade;
		
		//Other Variables
		public static string[] AllArgs;
		
		
		
		#region SteamBot Configuration
		/**
		 * 
		 * SteamBot Configuration
		 * Modify this section to your needs
		 * 
		 */
		
		//Name of the Bot
		public static string BotPersonaName = "[StǝamBot] GGC RaffleBot";
		
		//Default Persona State
		public static EPersonaState BotPersonaState = EPersonaState.LookingToTrade;
		
		#endregion
		
		
		
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
			#region SteamRE Init
			AllArgs = args;
			
			//Hacking around https
			ServicePointManager.CertificatePolicy = new MainClass ();
			
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine ("\n\tSteamBot Beta\n\tCreated by Jessecar96.\n\n");
			Console.ForegroundColor = ConsoleColor.White;
			
			
			steamClient = new SteamClient ();
			steamTrade = steamClient.GetHandler<SteamTrading>();
			SteamUser steamUser = steamClient.GetHandler<SteamUser> ();
			steamFriends = steamClient.GetHandler<SteamFriends>();
			
			steamClient.Connect ();
			#endregion
			
			
			while (true) {
				
				
				CallbackMsg msg = steamClient.WaitForCallback (true);
				
				//Console Debug
				printConsole (msg.ToString(),ConsoleColor.Blue,true);
				
				
				#region Logged Off Handler
				msg.Handle<SteamUser.LoggedOffCallback> (callback =>
				{
					printConsole("Logged Off: "+callback.Result,ConsoleColor.Red);
				});
				#endregion
				
				
				#region Steam Disconnect Handler
				msg.Handle<SteamClient.DisconnectedCallback> (callback =>
				{
					printConsole("Disconnected.",ConsoleColor.Red);
				});
				#endregion
				
				
				#region Steam Connect Handler
				
				/**
				 * --Steam Connection Callback
				 * 
				 * It's not needed to modify this section
				 */
				
				msg.Handle<SteamClient.ConnectedCallback> (callback =>
				{
					//Print Callback
					printConsole("Steam Connected Callback: "+callback.Result, ConsoleColor.Cyan);
					
					//Validate Result
					if(callback.Result==EResult.OK){
						
						//Get Steam Login Details
						printConsole("Username: ",ConsoleColor.Cyan);
						string user = Console.ReadLine();
						printConsole("Password: ",ConsoleColor.Cyan);
						Console.ForegroundColor = ConsoleColor.Black;
						string pass = Console.ReadLine();
						Console.ForegroundColor = ConsoleColor.White;
						
						printConsole("Getting Web Cookies...",ConsoleColor.Yellow);
						
						//Get Web Cookies
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
				#endregion
				
				
				#region Steam Login Handler
				//Logged in (or not)
				msg.Handle<SteamUser.LoggedOnCallback>( callback =>
        		{
					printConsole("Logged on callback: "+callback.Result, ConsoleColor.Cyan);
					
					if(callback.Result != EResult.OK){
						printConsole("Login Failed!",ConsoleColor.Red);
					}else{
						printConsole("Successfulyl Logged In!\nWelcome "+steamUser.SteamID,ConsoleColor.Green);
						
						//Set community status
						steamFriends.SetPersonaName(BotPersonaName);
						steamFriends.SetPersonaState(BotPersonaState);
					}
					
        		});
				#endregion
				
				
				#region Steam Trade Start
				/**
				 * 
				 * Steam Trading Handler
				 *  
				 */
				msg.Handle<SteamTrading.TradeStartSessionCallback>(call =>
				{
					
					//Trading
					trade = null;
					trade = new TradeSystem();
					trade.initTrade(steamUser.SteamID,call.Other,WebCookies);
					
				});
				#endregion
				
				#region Trade Requested Handler
				//Don't modify this
				msg.Handle<SteamTrading.TradeProposedCallback>( thing =>
				{
					//Trade Callback
					printConsole ("Trade Proposed Callback. Other: "+thing.Other+"\n");
					
					//Accept It
					steamTrade.RequestTrade(thing.Other);
					
				});
				#endregion

				msg.Handle<SteamFriends.PersonaStateCallback>(callback =>
                {
                    
					if(steamFriends.GetFriendRelationship(callback.FriendID)==EFriendRelationship.PendingInvitee){
						printConsole("[Friend] Friend Request Pending: " + callback.FriendID + "(" + steamFriends.GetFriendPersonaName(callback.FriendID) + ") - Accepted", ConsoleColor.Yellow);
						steamFriends.AddFriend(callback.FriendID);
					}
                });
				
				
				#region Steam Chat Handler
				/**
				 * 
				 * Steam Chat Handler
				 * 
				 */
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
				#endregion
				
		
			} //end while loop
			
			
		} //end Main method
		
		
		#region Misc Methods
		//Don't modify this
		static bool FindArg( string[] args, string arg )
        {
            foreach ( string potentialArg in args )
            {
                if ( potentialArg.IndexOf( arg, StringComparison.OrdinalIgnoreCase ) != -1 )
                    return true;
            }
            return false;
		}
		
		#endregion

		
	} //end class

		
} //end namespace

//Applejack is best Pony ;)