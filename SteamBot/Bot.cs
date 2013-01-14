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

        public SteamID steamId;

        /// <summary>
        /// Sets up the bot.
        /// </summary>
        /// <param name="botConfig">The configuration class for this one.</param>
        /// <param name="handler">The handler that will handle things like users.</param>
        /// <param name="username">The username the bot with authenticate with.</param>
        /// <param name="password">The password the bot will authenticate with.</param>
        public Bot(BotConfig botConfig, BotHandler handler)
        {
            if (botConfig.AppIds == null)
            {
                throw new ArgumentNullException("botConfig.AppIds", 
                    "Please check your config file, because botConfig.AppIds is not set.");
            }

            handler.bot = this;
            this.botConfig = botConfig;
            this.handler = handler;
        }

        public Bot(BotConfig botConfig, Type handler)
        {
            this.handler = (BotHandler) System.Activator.CreateInstance(handler);
            this.handler.bot = this;
            this.botConfig = botConfig;
        }

        public void Start()
        {
            handler.HandleBotConnection();
        }

        public void Exit()
        {
            handler.OnBotShutdown();
        }

        ~Bot()
        {
            Exit();
        }
    }
}
