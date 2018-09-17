using System;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using SteamBot.SteamGroups;
using SteamKit2;
using SteamTrade;
using SteamKit2.Internal;
using SteamTrade.TradeOffer;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SteamBot
{
    public class Bot : IDisposable
    {
        #region Bot delegates
        public delegate UserHandler UserHandlerCreator(Bot bot, SteamID id);
        #endregion

        #region Private readonly variables
        private readonly SteamUser.LogOnDetails logOnDetails;
        private readonly string schemaLang;
        private readonly string logFile;
        private readonly Dictionary<SteamID, UserHandler> userHandlers;
        private readonly Log.LogLevel consoleLogLevel;
        private readonly Log.LogLevel fileLogLevel;
        private readonly UserHandlerCreator createHandler;
        private readonly bool isProccess;
        private readonly BackgroundWorker botThread;
        private readonly CallbackManager steamCallbackManager;
        #endregion

        #region Private variables
        private Task<Inventory> myInventoryTask;
        private TradeManager tradeManager;
        private TradeOfferManager tradeOfferManager;
        private int tradePollingInterval;
        private int tradeOfferPollingIntervalSecs;
        private string myUserNonce;
        private string myUniqueId;
        private bool cookiesAreInvalid = true;
        private List<SteamID> friends;
        private bool disposed = false;
        private string consoleInput;
        private Thread tradeOfferThread;
        #endregion

        #region Public readonly variables
        /// <summary>
        /// Userhandler class bot is running.
        /// </summary>
        public readonly string BotControlClass;
        /// <summary>
        /// The display name of bot to steam.
        /// </summary>
        public readonly string DisplayName;
        /// <summary>
        /// The chat response from the config file.
        /// </summary>
        public readonly string ChatResponse;
        /// <summary>
        /// An array of admins for bot.
        /// </summary>
        public readonly IEnumerable<SteamID> Admins;
        public readonly SteamClient SteamClient;
        public readonly SteamUser SteamUser;
        public readonly SteamFriends SteamFriends;
        public readonly SteamTrading SteamTrade;
        public readonly SteamGameCoordinator SteamGameCoordinator;
        public readonly SteamNotifications SteamNotifications;
        /// <summary>
        /// The amount of time the bot will trade for.
        /// </summary>
        public readonly int MaximumTradeTime;
        /// <summary>
        /// The amount of time the bot will wait between user interactions with trade.
        /// </summary>
        public readonly int MaximumActionGap;
        /// <summary>
        /// The api key of bot.
        /// </summary>
        public readonly string ApiKey;
        public readonly SteamWeb SteamWeb;
        /// <summary>
        /// The prefix shown before bot's display name.
        /// </summary>
        public readonly string DisplayNamePrefix;
        /// <summary>
        /// The instance of the Logger for the bot.
        /// </summary>
        public readonly Log Log;
        #endregion

        #region Public variables
        public string AuthCode;
        public bool IsRunning;
        /// <summary>
        /// Is bot fully Logged in.
        /// Set only when bot did successfully Log in.
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>
        /// The current trade the bot is in.
        /// </summary>
        public Trade CurrentTrade { get; private set; }

        /// <summary>
        /// The current game bot is in.
        /// Default: 0 = No game.
        /// </summary>
        public int CurrentGame { get; private set; }

        public SteamAuth.SteamGuardAccount SteamGuardAccount;
        #endregion

        public IEnumerable<SteamID> FriendsList
        {
            get
            {
                CreateFriendsListIfNecessary();
                return friends;
            }
        }

        public Inventory MyInventory
        {
            get
            {
                myInventoryTask.Wait();
                return myInventoryTask.Result;
            }
        }

        public CallbackManager SteamCallbackManager { get { return steamCallbackManager; } }

        /// <summary>
        /// Compatibility sanity.
        /// </summary>
        [Obsolete("Refactored to be Log instead of log")]
        public Log log { get { return Log; } }

        public Bot(Configuration.BotInfo config, string apiKey, UserHandlerCreator handlerCreator, bool debug = false, bool process = false, bool useTwoFactorByDefault = false)
        {
            userHandlers = new Dictionary<SteamID, UserHandler>();
            logOnDetails = new SteamUser.LogOnDetails
            {
                Username = config.Username,
                Password = config.Password
            };
            DisplayName  = config.DisplayName;
            ChatResponse = config.ChatResponse;
            MaximumTradeTime = config.MaximumTradeTime;
            MaximumActionGap = config.MaximumActionGap;
            DisplayNamePrefix = config.DisplayNamePrefix;
            tradePollingInterval = config.TradePollingInterval <= 100 ? 800 : config.TradePollingInterval;
            tradeOfferPollingIntervalSecs = (config.TradeOfferPollingIntervalSecs == 0 ? 30 : config.TradeOfferPollingIntervalSecs);
            schemaLang = config.SchemaLang != null && config.SchemaLang.Length == 2 ? config.SchemaLang.ToLower() : "en";
            Admins = config.Admins;
            ApiKey = !String.IsNullOrEmpty(config.ApiKey) ? config.ApiKey : apiKey;
            isProccess = process;
            try
            {
                if( config.LogLevel != null )
                {
                    consoleLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.LogLevel, true);
                    Console.WriteLine(@"(Console) LogLevel configuration parameter used in bot {0} is depreciated and may be removed in future versions. Please use ConsoleLogLevel instead.", DisplayName);
                }
                else consoleLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.ConsoleLogLevel, true);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(@"(Console) ConsoleLogLevel invalid or unspecified for bot {0}. Defaulting to ""Info""", DisplayName);
                consoleLogLevel = Log.LogLevel.Info;
            }

            try
            {
                fileLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.FileLogLevel, true);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(@"(Console) FileLogLevel invalid or unspecified for bot {0}. Defaulting to ""Info""", DisplayName);
                fileLogLevel = Log.LogLevel.Info;
            }

            logFile = config.LogFile;
            Log = new Log(logFile, DisplayName, consoleLogLevel, fileLogLevel);
            if (useTwoFactorByDefault)
            {
                var mobileAuthCode = GetMobileAuthCode();
                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    Log.Error("Failed to generate 2FA code. Make sure you have linked the authenticator via SteamBot.");
                }
                else
                {
                    logOnDetails.TwoFactorCode = mobileAuthCode;
                    Log.Success("Generated 2FA code.");
                }
            }
            createHandler = handlerCreator;
            BotControlClass = config.BotControlClass;
            SteamWeb = new SteamWeb();

            // Hacking around https
            ServicePointManager.ServerCertificateValidationCallback += SteamWeb.ValidateRemoteCertificate;

            Log.Debug ("Initializing Steam Bot...");            
            SteamClient = new SteamClient();
            SteamClient.AddHandler(new SteamNotifications());
            steamCallbackManager = new CallbackManager(SteamClient);
            SubscribeSteamCallbacks();
            SteamTrade = SteamClient.GetHandler<SteamTrading>();
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();
            SteamGameCoordinator = SteamClient.GetHandler<SteamGameCoordinator>();
            SteamNotifications = SteamClient.GetHandler<SteamNotifications>();

            botThread = new BackgroundWorker { WorkerSupportsCancellation = true };
            botThread.DoWork += BackgroundWorkerOnDoWork;
            botThread.RunWorkerCompleted += BackgroundWorkerOnRunWorkerCompleted;
        }

        ~Bot()
        {
            Dispose(false);
        }

        private void CreateFriendsListIfNecessary()
        {
            if (friends != null)
                return;

            friends = new List<SteamID>();
            for (int i = 0; i < SteamFriends.GetFriendCount(); i++)
                friends.Add(SteamFriends.GetFriendByIndex(i));
        }

        /// <summary>
        /// Occurs when the bot needs the SteamGuard authentication code.
        /// </summary>
        /// <remarks>
        /// Return the code in <see cref="SteamGuardRequiredEventArgs.SteamGuard"/>
        /// </remarks>
        public event EventHandler<SteamGuardRequiredEventArgs> OnSteamGuardRequired;

        /// <summary>
        /// Starts the callback thread and connects to Steam via SteamKit2.
        /// </summary>
        /// <remarks>
        /// THIS NEVER RETURNS.
        /// </remarks>
        /// <returns><c>true</c>. See remarks</returns>
        public bool StartBot()
        {
            IsRunning = true;
            Log.Info("Connecting...");
            if (!botThread.IsBusy)
                botThread.RunWorkerAsync();
            SteamClient.Connect();
            Log.Success("Done Loading Bot!");
            return true; // never get here
        }

        /// <summary>
        /// Disconnect from the Steam network and stop the callback
        /// thread.
        /// </summary>
        public void StopBot()
        {
            IsRunning = false;
            Log.Debug("Trying to shut down bot thread.");
            SteamClient.Disconnect();
            botThread.CancelAsync();
            while (botThread.IsBusy)
                Thread.Yield();
            userHandlers.Clear();
        }

        /// <summary>
        /// Creates a new trade with the given partner.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if trade was opened,
        /// <c>false</c> if there is another trade that must be closed first.
        /// </returns>
        public bool OpenTrade (SteamID other)
        {
            if (CurrentTrade != null || CheckCookies() == false)
                return false;
            SteamTrade.Trade(other);
            return true;
        }

        /// <summary>
        /// Closes the current active trade.
        /// </summary>
        public void CloseTrade() 
        {
            if (CurrentTrade == null)
                return;
            UnsubscribeTrade (GetUserHandler (CurrentTrade.OtherSID), CurrentTrade);
            tradeManager.StopTrade ();
            CurrentTrade = null;
        }

        void OnTradeTimeout(object sender, EventArgs args) 
        {
            // ignore event params and just null out the trade.
            GetUserHandler(CurrentTrade.OtherSID).OnTradeTimeout();
        }

        /// <summary>
        /// Create a new trade offer with the specified partner
        /// </summary>
        /// <param name="other">SteamId of the partner</param>
        /// <returns></returns>
        public TradeOffer NewTradeOffer(SteamID other)
        {
            return tradeOfferManager.NewOffer(other);
        }

        /// <summary>
        /// Try to get a specific trade offer using the offerid
        /// </summary>
        /// <param name="offerId"></param>
        /// <param name="tradeOffer"></param>
        /// <returns></returns>
        public bool TryGetTradeOffer(string offerId, out TradeOffer tradeOffer)
        {
            return tradeOfferManager.TryGetOffer(offerId, out tradeOffer);
        }

        public void HandleBotCommand(string command)
        {
            try
            {
                if (command == "linkauth")
                {
                    LinkMobileAuth();
                }
                else if (command == "getauth")
                {
                    try
                    {
                        Log.Success("Generated Steam Guard code: " + SteamGuardAccount.GenerateSteamGuardCode());
                    }
                    catch (NullReferenceException)
                    {
                        Log.Error("Unable to generate Steam Guard code.");
                    }
                }
                else if (command == "unlinkauth")
                {
                    if (SteamGuardAccount == null)
                    {
                        Log.Error("Mobile authenticator is not active on this bot.");
                    }
                    else if (SteamGuardAccount.DeactivateAuthenticator())
                    {
                        Log.Success("Deactivated authenticator on this account.");
                    }
                    else
                    {
                        Log.Error("Failed to deactivate authenticator on this account.");
                    }
                }
                else
                {
                    GetUserHandler(SteamClient.SteamID).OnBotCommand(command);
                }
            }
            catch (ObjectDisposedException e)
            {
                // Writing to console because odds are the error was caused by a disposed Log.
                Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
                if (!this.IsRunning)
                {
                    Console.WriteLine("The Bot is no longer running and could not write to the Log. Try Starting this bot first.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
            }
        }

        protected void SpawnTradeOfferPollingThread()
        {
            if (tradeOfferThread == null)
            {
                tradeOfferThread = new Thread(TradeOfferPollingFunction);
                tradeOfferThread.Start();
            }
        }

        protected void CancelTradeOfferPollingThread()
        {
            tradeOfferThread = null;
        }

        protected void TradeOfferPollingFunction()
        {
            while (tradeOfferThread == Thread.CurrentThread)
            {
                try
                {
                    tradeOfferManager.EnqueueUpdatedOffers();
                }
                catch (Exception e)
                {
                    Log.Error("Error while polling trade offers: " + e);
                }

                Thread.Sleep(tradeOfferPollingIntervalSecs*1000);
            }
        }

        public void HandleInput(string input)
        {
            consoleInput = input;
        }

        public string WaitForInput()
        {
            consoleInput = null;
            while (true)
            {
                if (consoleInput != null)
                {
                    return consoleInput;
                }
                Thread.Sleep(5);
            }
        }

        bool HandleTradeSessionStart (SteamID other)
        {
            if (CurrentTrade != null)
                return false;
            try
            {
                tradeManager.InitializeTrade(SteamUser.SteamID, other);
                CurrentTrade = tradeManager.CreateTrade(SteamUser.SteamID, other);
                CurrentTrade.OnClose += CloseTrade;
                SubscribeTrade(CurrentTrade, GetUserHandler(other));
                tradeManager.StartTradeThread(CurrentTrade);
                return true;
            }
            catch (SteamTrade.Exceptions.InventoryFetchException)
            {
                // we shouldn't get here because the inv checks are also
                // done in the TradeProposedCallback handler.
                /*string response = String.Empty;
                if (ie.FailingSteamId.ConvertToUInt64() == other.ConvertToUInt64())
                {
                    response = "Trade failed. Could not correctly fetch your backpack. Either the inventory is inaccessible or your backpack is private.";
                }
                else 
                {
                    response = "Trade failed. Could not correctly fetch my backpack.";
                }
                
                SteamFriends.SendChatMessage(other, 
                                             EChatEntryType.ChatMsg,
                                             response);

                Log.Info ("Bot sent other: {0}", response);
                
                CurrentTrade = null;*/
                return false;
            }
        }

        public void SetGamePlaying(int id)
        {
            var gamePlaying = new SteamKit2.ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            if (id != 0)
                gamePlaying.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
                {
                    game_id = new GameID(id),
                });
            SteamClient.Send(gamePlaying);
            CurrentGame = id;
        }
        
        string GetMobileAuthCode()
        {
            var authFile = Path.Combine("authfiles", String.Format("{0}.auth", logOnDetails.Username));
            if (File.Exists(authFile))
            {
                SteamGuardAccount = Newtonsoft.Json.JsonConvert.DeserializeObject<SteamAuth.SteamGuardAccount>(File.ReadAllText(authFile));
                return SteamGuardAccount.GenerateSteamGuardCode();
            }
            return string.Empty;
        }

        /// <summary>
        /// Link a mobile authenticator to bot account, using SteamTradeOffersBot as the authenticator.
        /// Called from bot manager console. Usage: "exec [index] linkauth"
        /// If successful, 2FA will be required upon the next login.
        /// Use "exec [index] getauth" if you need to get a Steam Guard code for the account.
        /// To deactivate the authenticator, use "exec [index] unlinkauth".
        /// </summary>
        void LinkMobileAuth()
        {
            new Thread(() =>
            {
                var login = new SteamAuth.UserLogin(logOnDetails.Username, logOnDetails.Password);
                var loginResult = login.DoLogin();
                if (loginResult == SteamAuth.LoginResult.NeedEmail)
                {
                    while (loginResult == SteamAuth.LoginResult.NeedEmail)
                    {
                        Log.Interface("Enter Steam Guard code from email (type \"input [index] [code]\"):");
                        var emailCode = WaitForInput();
                        login.EmailCode = emailCode;
                        loginResult = login.DoLogin();
                    }
                }
                if (loginResult == SteamAuth.LoginResult.LoginOkay)
                {
                    Log.Info("Linking mobile authenticator...");
                    var authLinker = new SteamAuth.AuthenticatorLinker(login.Session);
                    var addAuthResult = authLinker.AddAuthenticator();
                    if (addAuthResult == SteamAuth.AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
                    {
                        while (addAuthResult == SteamAuth.AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
                        {
                            Log.Interface("Enter phone number with country code, e.g. +1XXXXXXXXXXX (type \"input [index] [number]\"):");
                            var phoneNumber = WaitForInput();
                            authLinker.PhoneNumber = phoneNumber;
                            addAuthResult = authLinker.AddAuthenticator();
                        }
                    }
                    if (addAuthResult == SteamAuth.AuthenticatorLinker.LinkResult.AwaitingFinalization)
                    {
                        SteamGuardAccount = authLinker.LinkedAccount;
                        try
                        {
                            var authFile = Path.Combine("authfiles", String.Format("{0}.auth", logOnDetails.Username));
                            Directory.CreateDirectory(Path.Combine(System.Windows.Forms.Application.StartupPath, "authfiles"));
                            File.WriteAllText(authFile, Newtonsoft.Json.JsonConvert.SerializeObject(SteamGuardAccount));
                            Log.Interface("Enter SMS code (type \"input [index] [code]\"):");
                            var smsCode = WaitForInput();
                            var authResult = authLinker.FinalizeAddAuthenticator(smsCode);
                            if (authResult == SteamAuth.AuthenticatorLinker.FinalizeResult.Success)
                            {
                                Log.Success("Linked authenticator.");
                            }
                            else
                            {
                                Log.Error("Error linking authenticator: " + authResult);
                            }
                        }
                        catch (IOException)
                        {
                            Log.Error("Failed to save auth file. Aborting authentication.");
                        }                        
                    }
                    else
                    {
                        Log.Error("Error adding authenticator: " + addAuthResult);
                    }
                }
                else
                {
                    if (loginResult == SteamAuth.LoginResult.Need2FA)
                    {
                        Log.Error("Mobile authenticator has already been linked!");
                    }
                    else
                    {
                        Log.Error("Error performing mobile login: " + loginResult);
                    }
                }
            }).Start();
        }

        void UserLogOn()
        {
            // get sentry file which has the machine hw info saved 
            // from when a steam guard code was entered
            Directory.CreateDirectory(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "sentryfiles"));
            FileInfo fi = new FileInfo(System.IO.Path.Combine("sentryfiles",String.Format("{0}.sentryfile", logOnDetails.Username)));

            if (fi.Exists && fi.Length > 0)
                logOnDetails.SentryFileHash = SHAHash(File.ReadAllBytes(fi.FullName));
            else
                logOnDetails.SentryFileHash = null;

            SteamUser.LogOn(logOnDetails);
        }

        void UserWebLogOn()
        {
            do
            {
                IsLoggedIn = SteamWeb.Authenticate(myUniqueId, SteamClient, myUserNonce);

                if(!IsLoggedIn)
                {
                    Log.Warn("Authentication failed, retrying in 2s...");
                    Thread.Sleep(2000);
                }
            } while(!IsLoggedIn);

            Log.Success("User Authenticated!");

            tradeManager = new TradeManager(ApiKey, SteamWeb);
            tradeManager.SetTradeTimeLimits(MaximumTradeTime, MaximumActionGap, tradePollingInterval);
            tradeManager.OnTimeout += OnTradeTimeout;
            tradeOfferManager = new TradeOfferManager(ApiKey, SteamWeb);
            SubscribeTradeOffer(tradeOfferManager);
            cookiesAreInvalid = false;

            // Success, check trade offers which we have received while we were offline
            SpawnTradeOfferPollingThread();
        }

        /// <summary>
        /// Checks if sessionId and token cookies are still valid.
        /// Sets cookie flag if they are invalid.
        /// </summary>
        /// <returns>true if cookies are valid; otherwise false</returns>
        bool CheckCookies()
        {
            // We still haven't re-authenticated
            if (cookiesAreInvalid)
                return false;

            try
            {
                if (!SteamWeb.VerifyCookies())
                {
                    // Cookies are no longer valid
                    Log.Warn("Cookies are invalid. Need to re-authenticate.");
                    cookiesAreInvalid = true;
                    SteamUser.RequestWebAPIUserNonce();
                    return false;
                }
            }
            catch
            {
                // Even if exception is caught, we should still continue.
                Log.Warn("Cookie check failed. http://steamcommunity.com is possibly down.");
            }

            return true;
        }

        public UserHandler GetUserHandler(SteamID sid)
        {
            if (!userHandlers.ContainsKey(sid))
                userHandlers[sid] = createHandler(this, sid);
            return userHandlers[sid];
        }

        void RemoveUserHandler(SteamID sid)
        {
            if (userHandlers.ContainsKey(sid))
                userHandlers.Remove(sid);
        }

        static byte [] SHAHash (byte[] input)
        {
            SHA1Managed sha = new SHA1Managed();
            
            byte[] output = sha.ComputeHash( input );
            
            sha.Clear();
            
            return output;
        }

        void OnUpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
        {
            byte[] hash = SHAHash (machineAuth.Data);

            Directory.CreateDirectory(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "sentryfiles"));

            File.WriteAllBytes (System.IO.Path.Combine("sentryfiles", String.Format("{0}.sentryfile", logOnDetails.Username)), machineAuth.Data);
            
            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,
                
                SentryFileHash = hash, // should be the sha1 hash of the sentry file we just wrote
                
                OneTimePassword = machineAuth.OneTimePassword, // not sure on this one yet, since we've had no examples of steam using OTPs
                
                LastError = 0, // result from win32 GetLastError
                Result = EResult.OK, // if everything went okay, otherwise ~who knows~
                JobID = machineAuth.JobID, // so we respond to the correct server job
            };
            
            // send off our response
            SteamUser.SendMachineAuthResponse (authResponse);
        }

        /// <summary>
        /// Gets the bot's inventory and stores it in MyInventory.
        /// </summary>
        /// <example> This sample shows how to find items in the bot's inventory from a user handler.
        /// <code>
        /// Bot.GetInventory(); // Get the inventory first
        /// foreach (var item in Bot.MyInventory.Items)
        /// {
        ///     if (item.Defindex == 5021)
        ///     {
        ///         // Bot has a key in its inventory
        ///     }
        /// }
        /// </code>
        /// </example>
        public void GetInventory()
        {
            myInventoryTask = Task.Factory.StartNew((Func<Inventory>) FetchBotsInventory);
        }

        public void TradeOfferRouter(TradeOffer offer)
        {
            GetUserHandler(offer.PartnerSteamId).OnTradeOfferUpdated(offer);
        }
        public void SubscribeTradeOffer(TradeOfferManager tradeOfferManager)
        {
            tradeOfferManager.OnTradeOfferUpdated += TradeOfferRouter;
        }

        //todo: should unsubscribe eventually...
        public void UnsubscribeTradeOffer(TradeOfferManager tradeOfferManager)
        {
            tradeOfferManager.OnTradeOfferUpdated -= TradeOfferRouter;
        }

        /// <summary>
        /// Subscribes all listeners of this to the trade.
        /// </summary>
        public void SubscribeTrade (Trade trade, UserHandler handler)
        {
            trade.OnAwaitingConfirmation += handler._OnTradeAwaitingConfirmation;
            trade.OnClose += handler.OnTradeClose;
            trade.OnError += handler.OnTradeError;
            trade.OnStatusError += handler.OnStatusError;
            //trade.OnTimeout += OnTradeTimeout;
            trade.OnAfterInit += handler.OnTradeInit;
            trade.OnUserAddItem += handler.OnTradeAddItem;
            trade.OnUserRemoveItem += handler.OnTradeRemoveItem;
            trade.OnMessage += handler.OnTradeMessageHandler;
            trade.OnUserSetReady += handler.OnTradeReadyHandler;
            trade.OnUserAccept += handler.OnTradeAcceptHandler;
        }
        
        /// <summary>
        /// Unsubscribes all listeners of this from the current trade.
        /// </summary>
        public void UnsubscribeTrade (UserHandler handler, Trade trade)
        {
            trade.OnAwaitingConfirmation -= handler._OnTradeAwaitingConfirmation;
            trade.OnClose -= handler.OnTradeClose;
            trade.OnError -= handler.OnTradeError;
            trade.OnStatusError -= handler.OnStatusError;
            //Trade.OnTimeout -= OnTradeTimeout;
            trade.OnAfterInit -= handler.OnTradeInit;
            trade.OnUserAddItem -= handler.OnTradeAddItem;
            trade.OnUserRemoveItem -= handler.OnTradeRemoveItem;
            trade.OnMessage -= handler.OnTradeMessageHandler;
            trade.OnUserSetReady -= handler.OnTradeReadyHandler;
            trade.OnUserAccept -= handler.OnTradeAcceptHandler;
        }

        /// <summary>
        /// Fetch the Bot's inventory and log a warning if it's private
        /// </summary>
        private Inventory FetchBotsInventory()
        {
            var inventory = Inventory.FetchInventory(SteamUser.SteamID, ApiKey, SteamWeb);
            if(inventory.IsPrivate)
            {
                Log.Warn("The bot's backpack is private! If your bot adds any items it will fail! Your bot's backpack should be Public.");
            }
            return inventory;
        }

        public void AcceptAllMobileTradeConfirmations()
        {
            if (SteamGuardAccount == null)
            {
                Log.Warn("Bot account does not have 2FA enabled.");
            }
            else
            {
                SteamGuardAccount.Session.SteamLogin = SteamWeb.Token;
                SteamGuardAccount.Session.SteamLoginSecure = SteamWeb.TokenSecure;
                try
                {
                    foreach (var confirmation in SteamGuardAccount.FetchConfirmations())
                    {
                        if (SteamGuardAccount.AcceptConfirmation(confirmation))
                        {
                            Log.Success("Confirmed {0}. (Confirmation ID #{1})", confirmation.Description, confirmation.ID);
                        }
                    }
                }
                catch (SteamAuth.SteamGuardAccount.WGTokenInvalidException)
                {
                    Log.Error("Invalid session when trying to fetch trade confirmations.");
                }
            }                        
        }

        /// <summary>
        /// Get duration of escrow in days. Call this before sending a trade offer.
        /// Credit to: https://www.reddit.com/r/SteamBot/comments/3w8j7c/code_getescrowduration_for_c/
        /// </summary>
        /// <param name="steamId">Steam ID of user you want to send a trade offer to</param>
        /// <param name="token">User's trade token. Can be an empty string if user is on bot's friends list.</param>
        /// <exception cref="NullReferenceException">Thrown when Steam returns an empty response.</exception>
        /// <exception cref="TradeOfferEscrowDurationParseException">Thrown when the user is unavailable for trade or Steam returns invalid data.</exception>
        /// <returns>TradeOfferEscrowDuration</returns>
        public TradeOfferEscrowDuration GetEscrowDuration(SteamID steamId, string token)
        {
            var url = "https://steamcommunity.com/tradeoffer/new/";

            var data = new System.Collections.Specialized.NameValueCollection();
            data.Add("partner", steamId.AccountID.ToString());
            if (!string.IsNullOrEmpty(token))
            {
                data.Add("token", token);
            }            

            var resp = SteamWeb.Fetch(url, "GET", data, false);
            if (string.IsNullOrWhiteSpace(resp))
            {
                throw new NullReferenceException("Empty response from Steam when trying to retrieve escrow duration.");
            }

            return ParseEscrowResponse(resp);
        }

        /// <summary>
        /// Get duration of escrow in days. Call this after receiving a trade offer.
        /// </summary>
        /// <param name="tradeOfferId">The ID of the trade offer</param>
        /// <exception cref="NullReferenceException">Thrown when Steam returns an empty response.</exception>
        /// <exception cref="TradeOfferEscrowDurationParseException">Thrown when the user is unavailable for trade or Steam returns invalid data.</exception>
        /// <returns>TradeOfferEscrowDuration</returns>
        public TradeOfferEscrowDuration GetEscrowDuration(string tradeOfferId)
        {
            var url = "http://steamcommunity.com/tradeoffer/" + tradeOfferId;

            var resp = SteamWeb.Fetch(url, "GET", null, false);
            if (string.IsNullOrWhiteSpace(resp))
            {
                throw new NullReferenceException("Empty response from Steam when trying to retrieve escrow duration.");
            }

            return ParseEscrowResponse(resp);
        }

        private TradeOfferEscrowDuration ParseEscrowResponse(string resp)
        {
            var myM = Regex.Match(resp, @"g_daysMyEscrow(?:[\s=]+)(?<days>[\d]+);", RegexOptions.IgnoreCase);
            var theirM = Regex.Match(resp, @"g_daysTheirEscrow(?:[\s=]+)(?<days>[\d]+);", RegexOptions.IgnoreCase);
            if (!myM.Groups["days"].Success || !theirM.Groups["days"].Success)
            {
                var steamErrorM = Regex.Match(resp, @"<div id=""error_msg"">([^>]+)<\/div>", RegexOptions.IgnoreCase);
                if (steamErrorM.Groups.Count > 1)
                {
                    var steamError = Regex.Replace(steamErrorM.Groups[1].Value.Trim(), @"\t|\n|\r", ""); ;
                    throw new TradeOfferEscrowDurationParseException(steamError);
                }
                else
                {
                    throw new TradeOfferEscrowDurationParseException(string.Empty);
                }
            }

            return new TradeOfferEscrowDuration()
            {
                DaysMyEscrow = int.Parse(myM.Groups["days"].Value),
                DaysTheirEscrow = int.Parse(theirM.Groups["days"].Value)
            };
        }

        public class TradeOfferEscrowDuration
        {
            public int DaysMyEscrow { get; set; }
            public int DaysTheirEscrow { get; set; }
        }
        public class TradeOfferEscrowDurationParseException : Exception
        {
            public TradeOfferEscrowDurationParseException() : base() { }
            public TradeOfferEscrowDurationParseException(string message) : base(message) { }
        }

        #region Background Worker Methods

        private void BackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            if (runWorkerCompletedEventArgs.Error != null)
            {
                Exception ex = runWorkerCompletedEventArgs.Error;

                Log.Error("Unhandled exceptions in bot {0} callback thread: {1} {2}",
                      DisplayName,
                      Environment.NewLine,
                      ex);

                Log.Info("This bot died. Stopping it..");
                //backgroundWorker.RunWorkerAsync();
                //Thread.Sleep(10000);
                StopBot();
                //StartBot();
            }
        }

        private void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            while (!botThread.CancellationPending)
            {
                try
                {
                    SteamCallbackManager.RunCallbacks();

                    if(tradeOfferManager != null)
                    {
                        tradeOfferManager.HandleNextPendingTradeOfferUpdate();
                    }

                    Thread.Sleep(1);
                }
                catch (WebException e)
                {
                    Log.Error("URI: {0} >> {1}", (e.Response != null && e.Response.ResponseUri != null ? e.Response.ResponseUri.ToString() : "unknown"), e.ToString());
                    System.Threading.Thread.Sleep(45000);//Steam is down, retry in 45 seconds.
                }
                catch (Exception e)
                {
                    Log.Error("Unhandled exception occurred in bot: " + e);
                }
            }
        }

        #endregion Background Worker Methods

        private void FireOnSteamGuardRequired(SteamGuardRequiredEventArgs e)
        {
            // Set to null in case this is another attempt
            this.AuthCode = null;

            EventHandler<SteamGuardRequiredEventArgs> handler = OnSteamGuardRequired;
            if (handler != null)
                handler(this, e);
            else
            {
                while (true)
                {
                    if (this.AuthCode != null)
                    {
                        e.SteamGuard = this.AuthCode;
                        break;
                    }

                    Thread.Sleep(5);
                }
            }
        }

        #region Group Methods

        /// <summary>
        /// Accepts the invite to a Steam Group
        /// </summary>
        /// <param name="group">SteamID of the group to accept the invite from.</param>
        private void AcceptGroupInvite(SteamID group)
        {
            var AcceptInvite = new ClientMsg<CMsgGroupInviteAction>((int)EMsg.ClientAcknowledgeClanInvite);

            AcceptInvite.Body.GroupID = group.ConvertToUInt64();
            AcceptInvite.Body.AcceptInvite = true;

            this.SteamClient.Send(AcceptInvite);
            
        }

        /// <summary>
        /// Declines the invite to a Steam Group
        /// </summary>
        /// <param name="group">SteamID of the group to decline the invite from.</param>
        private void DeclineGroupInvite(SteamID group)
        {
            var DeclineInvite = new ClientMsg<CMsgGroupInviteAction>((int)EMsg.ClientAcknowledgeClanInvite);

            DeclineInvite.Body.GroupID = group.ConvertToUInt64();
            DeclineInvite.Body.AcceptInvite = false;

            this.SteamClient.Send(DeclineInvite);
        }

        /// <summary>
        /// Invites a use to the specified Steam Group
        /// </summary>
        /// <param name="user">SteamID of the user to invite.</param>
        /// <param name="groupId">SteamID of the group to invite the user to.</param>
        public void InviteUserToGroup(SteamID user, SteamID groupId)
        {
            var InviteUser = new ClientMsg<CMsgInviteUserToGroup>((int)EMsg.ClientInviteUserToClan);

            InviteUser.Body.GroupID = groupId.ConvertToUInt64();
            InviteUser.Body.Invitee = user.ConvertToUInt64();
            InviteUser.Body.UnknownInfo = true;

            this.SteamClient.Send(InviteUser);
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            StopBot();
            if (disposing)
                Log.Dispose();
            disposed = true;
        }

        private void SubscribeSteamCallbacks()
        {
            #region Login
            steamCallbackManager.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                Log.Debug("Connection Callback: {0}", callback.Result);

                if (callback.Result == EResult.OK)
                {
                    UserLogOn();
                }
                else
                {
                    Log.Error("Failed to connect to Steam Community, trying again...");
                    SteamClient.Connect();
                }

            });

            steamCallbackManager.Subscribe<SteamUser.LoggedOnCallback>(callback =>
            {
                Log.Debug("Logged On Callback: {0}", callback.Result);

                if (callback.Result == EResult.OK)
                {
                    myUserNonce = callback.WebAPIUserNonce;
                }
                else
                {
                    Log.Error("Login Error: {0}", callback.Result);
                }

                if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
                {
                    var mobileAuthCode = GetMobileAuthCode();
                    if (string.IsNullOrEmpty(mobileAuthCode))
                    {
                        Log.Error("Failed to generate 2FA code. Make sure you have linked the authenticator via SteamBot.");
                    }
                    else
                    {
                        logOnDetails.TwoFactorCode = mobileAuthCode;
                        Log.Success("Generated 2FA code.");
                    }
                }
                else if (callback.Result == EResult.TwoFactorCodeMismatch)
                {
                    SteamAuth.TimeAligner.AlignTime();
                    logOnDetails.TwoFactorCode = SteamGuardAccount.GenerateSteamGuardCode();
                    Log.Success("Regenerated 2FA code.");
                }
                else if (callback.Result == EResult.AccountLogonDenied)
                {
                    Log.Interface("This account is SteamGuard enabled. Enter the code via the `auth' command.");

                    // try to get the steamguard auth code from the event callback
                    var eva = new SteamGuardRequiredEventArgs();
                    FireOnSteamGuardRequired(eva);
                    if (!String.IsNullOrEmpty(eva.SteamGuard))
                        logOnDetails.AuthCode = eva.SteamGuard;
                    else
                        logOnDetails.AuthCode = Console.ReadLine();
                }
                else if (callback.Result == EResult.InvalidLoginAuthCode)
                {
                    Log.Interface("The given SteamGuard code was invalid. Try again using the `auth' command.");
                    logOnDetails.AuthCode = Console.ReadLine();
                }
            });

            steamCallbackManager.Subscribe<SteamUser.LoginKeyCallback>(callback =>
            {
                myUniqueId = callback.UniqueID.ToString();

                UserWebLogOn();

                if (Trade.CurrentSchema == null)
                {
                    Log.Info("Downloading Schema...");
                    Trade.CurrentSchema = Schema.FetchSchema(ApiKey, schemaLang);
                    Log.Success("Schema Downloaded!");
                }

                SteamFriends.SetPersonaName(DisplayNamePrefix + DisplayName);
                SteamFriends.SetPersonaState(EPersonaState.Online);

                Log.Success("Steam Bot Logged In Completely!");

                GetUserHandler(SteamClient.SteamID).OnLoginCompleted();
            });

            steamCallbackManager.Subscribe<SteamUser.WebAPIUserNonceCallback>(webCallback =>
            {
                Log.Debug("Received new WebAPIUserNonce.");

                if (webCallback.Result == EResult.OK)
                {
                    myUserNonce = webCallback.Nonce;
                    UserWebLogOn();
                }
                else
                {
                    Log.Error("WebAPIUserNonce Error: " + webCallback.Result);
                }
            });

            steamCallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(
                authCallback => OnUpdateMachineAuthCallback(authCallback)
            );
            #endregion

            #region Friends
            steamCallbackManager.Subscribe<SteamFriends.FriendsListCallback>(callback =>
            {
                foreach (SteamFriends.FriendsListCallback.Friend friend in callback.FriendList)
                {
                    switch (friend.SteamID.AccountType)
                    {
                        case EAccountType.Clan:
                            if (friend.Relationship == EFriendRelationship.RequestRecipient)
                            {
                                if (GetUserHandler(friend.SteamID).OnGroupAdd())
                                {
                                    AcceptGroupInvite(friend.SteamID);
                                }
                                else
                                {
                                    DeclineGroupInvite(friend.SteamID);
                                }
                            }
                            break;
                        default:
                            CreateFriendsListIfNecessary();
                            if (friend.Relationship == EFriendRelationship.None)
                            {
                                friends.Remove(friend.SteamID);
                                GetUserHandler(friend.SteamID).OnFriendRemove();
                                RemoveUserHandler(friend.SteamID);
                            }
                            else if (friend.Relationship == EFriendRelationship.RequestRecipient)
                            {
                                if (GetUserHandler(friend.SteamID).OnFriendAdd())
                                {
                                    if (!friends.Contains(friend.SteamID))
                                    {
                                        friends.Add(friend.SteamID);
                                    }
                                    SteamFriends.AddFriend(friend.SteamID);
                                }
                                else
                                {
                                    if (friends.Contains(friend.SteamID))
                                    {
                                        friends.Remove(friend.SteamID);
                                    }
                                    SteamFriends.RemoveFriend(friend.SteamID);
                                    RemoveUserHandler(friend.SteamID);
                                }
                            }
                            break;
                    }
                }
            });


            steamCallbackManager.Subscribe<SteamFriends.FriendMsgCallback>(callback =>
            {
                EChatEntryType type = callback.EntryType;

                if (callback.EntryType == EChatEntryType.ChatMsg)
                {
                    Log.Info("Chat Message from {0}: {1}",
                                         SteamFriends.GetFriendPersonaName(callback.Sender),
                                         callback.Message
                                         );
                    GetUserHandler(callback.Sender).OnMessageHandler(callback.Message, type);
                }
            });
            #endregion

            #region Group Chat
            steamCallbackManager.Subscribe<SteamFriends.ChatMsgCallback>(callback =>
            {
                GetUserHandler(callback.ChatterID).OnChatRoomMessage(callback.ChatRoomID, callback.ChatterID, callback.Message);
            });
            #endregion

            #region Trading
            steamCallbackManager.Subscribe<SteamTrading.SessionStartCallback>(callback =>
            {
                bool started = HandleTradeSessionStart(callback.OtherClient);

                if (!started)
                    Log.Error("Could not start the trade session.");
                else
                    Log.Debug("SteamTrading.SessionStartCallback handled successfully. Trade Opened.");
            });

            steamCallbackManager.Subscribe<SteamTrading.TradeProposedCallback>(callback =>
            {
                if (CheckCookies() == false)
                {
                    SteamTrade.RespondToTrade(callback.TradeID, false);
                    return;
                }

                try
                {
                    tradeManager.InitializeTrade(SteamUser.SteamID, callback.OtherClient);
                }
                catch (WebException we)
                {
                    SteamFriends.SendChatMessage(callback.OtherClient,
                             EChatEntryType.ChatMsg,
                             "Trade error: " + we.Message);

                    SteamTrade.RespondToTrade(callback.TradeID, false);
                    return;
                }
                catch (Exception)
                {
                    SteamFriends.SendChatMessage(callback.OtherClient,
                             EChatEntryType.ChatMsg,
                             "Trade declined. Could not correctly fetch your backpack.");

                    SteamTrade.RespondToTrade(callback.TradeID, false);
                    return;
                }

                //if (tradeManager.OtherInventory.IsPrivate)
                //{
                //    SteamFriends.SendChatMessage(callback.OtherClient, 
                //                                 EChatEntryType.ChatMsg,
                //                                 "Trade declined. Your backpack cannot be private.");

                //    SteamTrade.RespondToTrade (callback.TradeID, false);
                //    return;
                //}

                if (CurrentTrade == null && GetUserHandler(callback.OtherClient).OnTradeRequest())
                    SteamTrade.RespondToTrade(callback.TradeID, true);
                else
                    SteamTrade.RespondToTrade(callback.TradeID, false);
            });

            steamCallbackManager.Subscribe<SteamTrading.TradeResultCallback>(callback =>
            {
                if (callback.Response == EEconTradeResponse.Accepted)
                {
                    Log.Debug("Trade Status: {0}", callback.Response);
                    Log.Info("Trade Accepted!");
                    GetUserHandler(callback.OtherClient).OnTradeRequestReply(true, callback.Response.ToString());
                }
                else
                {
                    Log.Warn("Trade failed: {0}", callback.Response);
                    CloseTrade();
                    GetUserHandler(callback.OtherClient).OnTradeRequestReply(false, callback.Response.ToString());
                }

            });
            #endregion

            #region Disconnect
            steamCallbackManager.Subscribe<SteamUser.LoggedOffCallback>(callback =>
            {
                IsLoggedIn = false;
                Log.Warn("Logged off Steam.  Reason: {0}", callback.Result);
                CancelTradeOfferPollingThread();
            });

            steamCallbackManager.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                if (IsLoggedIn)
                {
                    IsLoggedIn = false;
                    CloseTrade();
                    Log.Warn("Disconnected from Steam Network!");
                    CancelTradeOfferPollingThread();
                }

                SteamClient.Connect();
            });
            #endregion

            #region Notifications
            steamCallbackManager.Subscribe<SteamBot.SteamNotifications.CommentNotificationCallback>(callback =>
            {
                //various types of comment notifications on profile/activity feed etc
                //Log.Info("received CommentNotificationCallback");
                //Log.Info("New Commments " + callback.CommentNotifications.CountNewComments);
                //Log.Info("New Commments Owners " + callback.CommentNotifications.CountNewCommentsOwner);
                //Log.Info("New Commments Subscriptions" + callback.CommentNotifications.CountNewCommentsSubscriptions);
            });
            #endregion
        }

    }
}
