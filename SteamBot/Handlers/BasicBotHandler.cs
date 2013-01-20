using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using SteamBot.Trading;

namespace SteamBot.Handlers
{
    public class BasicBotHandler : BotHandler
    {

        private volatile bool running = true;
        private IBotRunner log;
        private Trading.Trade currentTrade { get; set; }

        List<SteamID> friends = new List<SteamID>();

        public override void HandleBotConnection()
        {
            logOnDetails = new SteamUser.LogOnDetails
            {
                Username = bot.botConfig.Username,
                Password = bot.botConfig.Password
            };
            steamClient = new SteamClient ();
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            steamTrading = steamClient.GetHandler<SteamTrading>();
            log = bot.botConfig.runner;

            manager = new CallbackManager(steamClient);

            new Callback<SteamClient.ConnectedCallback>(OnClientConnected, manager);
            new Callback<SteamClient.DisconnectedCallback>(OnClientDisconnect, manager);
            new Callback<SteamClient.JobCallback<SteamUser.UpdateMachineAuthCallback>>((jobCallback) =>
            {
                OnUserUpdateMachineAuth(jobCallback.Callback, jobCallback.JobID);
            }, manager);

            new Callback<SteamUser.LoggedOnCallback>(OnUserLoggedOn, manager);
            new Callback<SteamUser.LoginKeyCallback>(OnUserLoginKey, manager);
            new Callback<SteamUser.LoggedOffCallback>(OnUserLoggedOff, manager);

            new Callback<SteamFriends.FriendMsgCallback>(OnFriendMsg, manager);
            new Callback<SteamFriends.FriendsListCallback>(OnFriendsListUpdate, manager);
            new Callback<SteamFriends.PersonaStateCallback>(OnFriendsPersonaStateUpdate, manager);

            new Callback<SteamTrading.SessionStartCallback>(OnTradingSessionStart, manager);
            new Callback<SteamTrading.TradeProposedCallback>(OnTradeProposed, manager);
            new Callback<SteamTrading.TradeResultCallback>(OnTradeResult, manager);

            steamClient.Connect ();
            DoLog (ELogType.INFO, "Connecting...");

            while (running)
            {
                manager.RunWaitCallbacks(new TimeSpan(1));
            }
        }

        public override void OnClientConnected(SteamClient.ConnectedCallback callback)
        {
            if(callback.Result == EResult.OK)
            {
                DoLog(ELogType.SUCCESS, "Connected!");

                // get sentry file which has the machine hw info saved 
                // from when a steam guard code was entered
                FileInfo fi = new FileInfo(bot.botConfig.SentryFile);

                if (fi.Exists && fi.Length > 0)
                    logOnDetails.SentryFileHash = SHAHash(File.ReadAllBytes(fi.FullName));
                else
                    logOnDetails.SentryFileHash = null;

                steamUser.LogOn(logOnDetails);
            }
            else
            {
                DoLog(ELogType.ERROR, "Could not connect to Steam :(");
            }
        }

        public override void OnUserLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result == EResult.OK)
            {
                DoLog(ELogType.SUCCESS, "Login Completed Successfully.");
                bot.steamId = callback.ClientSteamID;
            }
            else if (callback.Result == EResult.InvalidLoginAuthCode ||
                    callback.Result == EResult.AccountLogonDenied)
            {
                DoLog(ELogType.INTERFACE, "Requires SteamGuard code:");
                logOnDetails.AuthCode = log.GetSteamGuardCode();
                DoLog(ELogType.INFO, String.Format("Using Code {0}", logOnDetails.AuthCode));
            }
            else
            {
                DoLog(ELogType.ERROR, String.Format("Login Failed: {0}", callback.Result));
            }
        }

        public override void OnUserLoginKey(SteamUser.LoginKeyCallback callback)
        {
            steamFriends.SetPersonaName(bot.botConfig.BotName);
            steamFriends.SetPersonaState(EPersonaState.Online);

            web = new Trading.Web();
            Trading.IAuthenticator authenticator = (Trading.IAuthenticator) System.Activator.CreateInstance(bot.botConfig.Authenticator);
            authenticator.handler = this;
            authenticator.loginKeyCallback = callback;
            authenticator.web = web;
            string[] result = authenticator.Authenticate();
            sessionId = result[0];
            steamLogin = result[1];
            web.Cookies.Add(new Cookie("sessionid", sessionId, String.Empty, "steamcommunity.com"));
            web.Cookies.Add(new Cookie("steamLogin", steamLogin, String.Empty, "steamcommunity.com"));

            DoLog(ELogType.SUCCESS, "Logged in!");
        }

        private void OnUserUpdateMachineAuth(SteamUser.UpdateMachineAuthCallback machineAuth, JobID jobId)
        {
            byte[] hash = SHAHash(machineAuth.Data);
            File.WriteAllBytes(bot.botConfig.SentryFile, machineAuth.Data);

            SteamUser.MachineAuthDetails authDetails = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,
                OneTimePassword = machineAuth.OneTimePassword,
                SentryFileHash = hash, // SHA1 of the SentryFile
                LastError = 0,
                Result = EResult.OK,
                JobID = jobId
            };
            steamUser.SendMachineAuthResponse(authDetails);
        }

        public override void OnClientDisconnect(SteamClient.DisconnectedCallback callback)
        {
            if (running)
            {
                DoLog(ELogType.WARN, "Disconnected from network, retrying...");
                steamClient.Connect();
            }
            else
            {
                DoLog(ELogType.SUCCESS, "SUCCESSFULLY DISCONNECTED!");
            }
        }

        public override void OnUserLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            if (running)
            {
                throw new NotImplementedException();
            }
            else
            {
                steamClient.Disconnect();
            }
        }

        public override void OnBotShutdown()
        {
            running = false;
            steamUser.LogOff();
            if (currentTrade != null)
            {
                currentTrade.CloseTrade();
            }
        }

        public override void OnFriendMsg(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.EntryType == EChatEntryType.Emote ||
                callback.EntryType == EChatEntryType.ChatMsg)
            {
                steamFriends.SendChatMessage(callback.Sender, callback.EntryType, callback.Message);
                DoLog(ELogType.INFO, String.Format("Recieved Message from {0}: {1}", callback.Sender, callback.Message));
            }
        }

        public override void OnFriendAdd(SteamID steamId)
        {
            steamFriends.AddFriend(steamId);
            DoLog(ELogType.INFO, "Recieved friend request from " + steamFriends.GetFriendPersonaName(steamId));
        }

        public override void OnFriendsListUpdate(SteamFriends.FriendsListCallback callback)
        {
            foreach (SteamFriends.FriendsListCallback.Friend friend in callback.FriendList)
            {
                if (!friends.Contains(friend.SteamID))
                {
                    friends.Add(friend.SteamID);
                }

                if (friend.Relationship == EFriendRelationship.RequestInitiator)
                {
                    OnFriendAdd(friend.SteamID);
                }
            }
        }

        public override void OnTradingSessionStart(SteamTrading.SessionStartCallback callback)
        {
            CreateTrade(callback.OtherClient);
        }

        public override void OnTradeProposed(SteamTrading.TradeProposedCallback callback)
        {
            if (currentTrade == null)
                steamTrading.RespondToTrade(callback.TradeID, true);
        }

        public override void OnTradeResult(SteamTrading.TradeResultCallback callback)
        {
            if (callback.Response != EEconTradeResponse.Accepted)
            {
                steamFriends.SendChatMessage(callback.OtherClient, EChatEntryType.ChatMsg, ":(");
            }
        }

        void CreateTrade(SteamID steamId)
        {
            if (currentTrade == null)
            {
                currentTrade = new Trading.Trade(steamId, bot);
            }
        }

        public override void HandleTradeClose(Api.ETradeStatus status)
        {
            DoLog(ELogType.WARN, String.Format("Trade Closed: {0}", status));
            steamFriends.SendChatMessage(currentTrade.otherSid, EChatEntryType.ChatMsg, ":(");
            currentTrade = null;
        }

        static byte[] SHAHash(byte[] input)
        {
            SHA1Managed sha = new SHA1Managed();
            byte[] output = sha.ComputeHash(input);
            sha.Clear();
            return output;
        }

        void DoLog(ELogType type, string logString)
        {
           log.DoLog(type, bot.botConfig.BotName, logString);
        }
    }
}
