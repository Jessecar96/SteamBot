using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot
{
    public class Bot
    {
        /// <summary>
        /// This contains the <see cref="BotConfig"/> class.
        /// </summary>
        public BotConfig botConfig;

        /// <summary>
        /// This contains the handler for this class.
        /// </summary>
        public BotHandler handler;

        /// <summary>
        /// Sets up the bot.
        /// </summary>
        /// <param name="botConfig">The configuration class for this one.</param>
        /// <param name="handler">The handler that will handle things like users.</param>
        /// <param name="username">The username the bot with authenticate with.</param>
        /// <param name="password">The password the bot will authenticate with.</param>
        Bot(BotConfig botConfig, BotHandler handler)
        {
            handler.bot = this;
            this.botConfig = botConfig;
            this.handler = handler;
        }

        public static Bot InitializeBot(BotConfig botConfig, Type handler)
        {
            BotHandler botHandler = (BotHandler) System.Activator.CreateInstance(handler);
            Bot bot = new Bot(botConfig, botHandler);
            return bot;
        }

        public void Start()
        {
            handler.HandleBotConnection();
        }

        public void Exit()
        {
            handler.HandleBotShutdown();
        }

        ~Bot()
        {
            Exit();
        }

        /// <summary>
        /// Takes all steam messages and delegates them to the BotHandler.
        /// </summary>
        /// <param name="msg"></param>
        public void HandleSteamMessage(CallbackMsg msg)
        {
            botConfig.runner.DoLog(ELogType.DEBUG, botConfig.BotName, msg.ToString());
            #region Connection
            msg.Handle<SteamClient.ConnectedCallback>(callback =>
            {
                botConfig.runner.DoLog (ELogType.DEBUG, String.Format ("Callback Result: {0}", callback.Result.ToString ()));
                handler.HandleBotLogin(callback);
            });

            msg.Handle<SteamClient.DisconnectedCallback>(callback =>
            {
                handler.HandleBotDisconnect();
            });
            #endregion

            #region Login
            msg.Handle<SteamUser.LoggedOnCallback>(callback =>
            {
                handler.HandleBotLogin(callback);
            });

            msg.Handle<SteamUser.LoginKeyCallback>(callback =>
            {
                handler.HandleBotLogin(callback);
            });

            msg.Handle<SteamUser.LoggedOffCallback>(callback =>
            {
                handler.HandleBotLogoff(callback);
            });

            if (msg.IsType<SteamClient.JobCallback<SteamUser.UpdateMachineAuthCallback>>())
            {
                msg.Handle<SteamClient.JobCallback<SteamUser.UpdateMachineAuthCallback>>(
                    jobCallback => handler.HandleUpdateMachineAuth(jobCallback.Callback, jobCallback.JobID)
                );
            }
            #endregion

            #region Friends
            msg.Handle<SteamFriends.PersonaStateCallback>(callback =>
            {
                handler.HandleFriendPersonaState(callback);
            });

            msg.Handle<SteamFriends.FriendsListCallback>(callback =>
            {
                handler.HandleFriendsList(callback);
            });

            msg.Handle<SteamFriends.FriendMsgCallback>(callback =>
            {
                handler.HandleFriendMsg(callback);
            });
            #endregion
        }
    }
}
