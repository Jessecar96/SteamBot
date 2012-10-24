using System;
using System.Text;
using System.Net;
using System.Threading;
using SteamKit2;

namespace SteamBot
{
    public class Bot
    {
		public bool IsLoggedIn = false;
		
		public string DisplayName { get; private set; }
		public string ChatResponse;
		public ulong[] Admins;
		
        public SteamFriends SteamFriends;
        public SteamClient SteamClient;
        public SteamTrading SteamTrade;
        public SteamUser SteamUser;
		
		public Trade CurrentTrade;
		public Trade.TradeListener TradeListener;

		public bool IsDebugMode = false;

		string Username;
		string Password;
		string apiKey;
        string sessionId;
        string token;

		public Bot(Configuration.BotInfo config, string apiKey, bool debug = false)
        {
			Username = config.Username;
			Password = config.Password;
			DisplayName = config.DisplayName;
			ChatResponse = config.ChatResponse;
			Admins = config.Admins;
			this.apiKey = apiKey;

			TradeListener = new TradeEnterTradeListener(this);

            // Hacking around https
			ServicePointManager.ServerCertificateValidationCallback += SteamWeb.ValidateRemoteCertificate; 

            SteamClient = new SteamClient();
            SteamTrade = SteamClient.GetHandler<SteamTrading>();
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();

            SteamClient.Connect();

            while (true)
            {
                Update();
            }
        }

		public void Update ()
		{
            var before = DateTime.Now;
            var msg = SteamClient.WaitForCallback(true, new TimeSpan(0, 0, 0, 0, 600));
            while(msg != null) {
                HandleSteamMessage (msg);
                msg = SteamClient.GetCallback (true);
            }

            TimeSpan span = DateTime.Now - before;
            if (span.TotalMilliseconds < 800)
                Thread.Sleep (span.TotalMilliseconds);
            if (CurrentTrade != null) {
			    try {
				    CurrentTrade.Poll ();
				} catch (Exception e) {
					Console.Write ("Error polling the trade: ");
					Console.WriteLine (e);
				}
			}
		}

        void HandleSteamMessage (CallbackMsg msg)
		{
			#region Login
			msg.Handle<SteamClient.ConnectedCallback> (callback =>
			{
				PrintConsole ("Connection Callback: " + callback.Result, ConsoleColor.Magenta);

				if (callback.Result == EResult.OK) {
					SteamUser.LogOn (new SteamUser.LogOnDetails
					     {
						Username = Username,
						Password = Password
					});
				} else {
					PrintConsole ("Failed to Connect to the steam community!\n", ConsoleColor.Red);
					SteamClient.Connect ();
				}
				
			});

			msg.Handle<SteamUser.LoggedOnCallback> (callback =>
			{
				PrintConsole ("Logged on callback: " + callback.Result, ConsoleColor.Magenta);
				
				if (callback.Result != EResult.OK) {
					PrintConsole ("Login Failure: " + callback.Result, ConsoleColor.Red);
				}
			});

			msg.Handle<SteamUser.LoginKeyCallback> (callback =>
			{
				while (true) {
					if (Authenticate (callback)) {
						PrintConsole ("Authenticated.");
						break;
					} else {
						PrintConsole ("Retrying auth...", ConsoleColor.Red);
						Thread.Sleep (2000);
					}
				}

				PrintConsole ("Downloading schema...", ConsoleColor.Magenta);

				Trade.CurrentSchema = Schema.FetchSchema (apiKey);

				PrintConsole ("All Done!", ConsoleColor.Magenta);

				SteamFriends.SetPersonaName ("[SteamBot] "+DisplayName);
				SteamFriends.SetPersonaState (EPersonaState.LookingToTrade);

				PrintConsole ("Successfully Logged In!\nWelcome " + SteamUser.SteamID + "\n\n", ConsoleColor.Magenta);

				IsLoggedIn = true;
			});
			#endregion

			#region Friends
			msg.Handle<SteamFriends.PersonaStateCallback> (callback =>
			{
				SteamFriends.AddFriend (callback.FriendID);
			});

			msg.Handle<SteamFriends.FriendMsgCallback> (callback =>
			{
				//Type (emote or chat)
				EChatEntryType type = callback.EntryType;
				
				if (type == EChatEntryType.ChatMsg) {
					PrintConsole ("[Chat] " + SteamFriends.GetFriendPersonaName (callback.Sender) + ": " + callback.Message, ConsoleColor.Magenta);
					
					string message = callback.Message;
					
					string response = ChatResponse;
					SteamFriends.SendChatMessage (callback.Sender, EChatEntryType.ChatMsg, response);
				}
				
			});
			#endregion

			#region Trading
			msg.Handle<SteamTrading.TradeStartSessionCallback> (call =>
			{
				CurrentTrade = new Trade (SteamUser.SteamID, call.Other, sessionId, token, apiKey, TradeListener);
                CurrentTrade.OnTimeout += () => {
                    CurrentTrade = null;
                };
			});

			msg.Handle<SteamTrading.TradeProposedCallback> (thing =>
			{
				SteamTrade.RequestTrade (thing.Other);
			});

			msg.Handle<SteamTrading.TradeRequestCallback> (thing =>
			{
				PrintConsole ("Trade Status: " + thing.Status, ConsoleColor.Magenta);

				if (thing.Status == ETradeStatus.Accepted) {
					PrintConsole ("Trade accepted!", ConsoleColor.Magenta);
				}
			});
			#endregion

			#region Disconnect
			msg.Handle<SteamUser.LoggedOffCallback> (callback => 
			{ 
				PrintConsole ("[SteamRE] Logged Off: " + callback.Result, ConsoleColor.Magenta);
			});

			msg.Handle<SteamClient.DisconnectedCallback> (callback =>
			{
				IsLoggedIn = false;
				if (CurrentTrade != null) {
					CurrentTrade = null;
				}
				PrintConsole ("[SteamRE] Disconnected from Steam Network!", ConsoleColor.Magenta);
				SteamClient.Connect ();
			});
			#endregion
		}
		
		// Authenticate. This does the same as SteamWeb.DoLogin(),
		// but without contacting the Steam Website.
		// Should this one doesnt work anymore, use SteamWeb.DoLogin().
        bool Authenticate (SteamUser.LoginKeyCallback callback)
		{
			sessionId = WebHelpers.EncodeBase64 (callback.UniqueID.ToString ());

			PrintConsole ("Got login key, performing web auth...");

			using (dynamic userAuth = WebAPI.GetInterface("ISteamUserAuth")) {
				// generate an AES session key
				var sessionKey = CryptoHelper.GenerateRandomBlock (32);

				// rsa encrypt it with the public key for the universe we're on
				byte[] cryptedSessionKey = null;
				using (var rsa = new RSACrypto(KeyDictionary.GetPublicKey(SteamClient.ConnectedUniverse))) {
					cryptedSessionKey = rsa.Encrypt (sessionKey);
				}

				var loginKey = new byte[20];
				Array.Copy (Encoding.ASCII.GetBytes (callback.LoginKey), loginKey, callback.LoginKey.Length);

				// aes encrypt the loginkey with our session key
				byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt (loginKey, sessionKey);

				KeyValue authResult;

				try {
					authResult = userAuth.AuthenticateUser (
                        steamid: SteamClient.SteamID.ConvertToUInt64 (),
                        sessionkey: WebHelpers.UrlEncode (cryptedSessionKey),
                        encrypted_loginkey: WebHelpers.UrlEncode (cryptedLoginKey),
                        method: "POST"
					);
				} catch (Exception) {
					return false;
				}

				token = authResult ["token"].AsString ();

				return true;
			}
		}

		protected void PrintConsole(String line, ConsoleColor color = ConsoleColor.White, bool isDebug = false)
		{
			Console.ForegroundColor = color;
			if (isDebug && IsDebugMode)
			{
				Console.WriteLine(line);
			}
			else
			{
				Console.WriteLine(line);
			}
			Console.ForegroundColor = ConsoleColor.White;
		}


    }
}