using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SteamBot
{
    public class Configuration
    {
        public static Configuration LoadConfiguration (string filename)
        {
            TextReader reader = new StreamReader(filename);
            string json = reader.ReadToEnd();
            reader.Close();

            Configuration config =  JsonConvert.DeserializeObject<Configuration>(json);

            config.Admins = config.Admins ?? new ulong[0];

            // merge bot-specific admins with global admins
            foreach (BotInfo bot in config.Bots)
            {
                if (bot.Admins == null)
                {
                    bot.Admins = new ulong[config.Admins.Length];
                    Array.Copy(config.Admins, bot.Admins, config.Admins.Length);
                }
                else
                {
                    bot.Admins = bot.Admins.Concat(config.Admins).ToArray();
                }
            }

            return config;
        }

        #region Top-level config properties
        
        /// <summary>
        /// Gets or sets the admins.
        /// </summary>
        /// <value>
        /// An array of Steam Profile IDs (64 bit IDs) of the users that are an 
        /// Admin of your bot(s). Each Profile ID should be a string in quotes 
        /// and separated by a comma. These admins are global to all bots 
        /// listed in the Bots array.
        /// </value>
        public ulong[] Admins { get; set; }

        /// <summary>
        /// Gets or sets the bots array.
        /// </summary>
        /// <value>
        /// The Bots object is an array of BotInfo objects containing
        ///  information about each individual bot you will be running. 
        /// </value>
        public BotInfo[] Bots { get; set; }

        /// <summary>
        /// Gets or sets YOUR API key.
        /// </summary>
        /// <value>
        /// The API key you have been assigned by Valve. If you do not have 
        /// one, it can be requested from Value at their Web API Key page. This
        /// is required and the bot(s) will not work without an API Key. 
        /// </value>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the main log file name.
        /// </summary>
        public string MainLog { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use separate processes.
        /// </summary>
        /// <value>
        /// <c>true</c> if bot manager is to open each bot in it's own process;
        /// otherwise, <c>false</c> to open each bot in a separate thread.
        /// Default is <c>false</c>.
        /// </value>
        public bool UseSeparateProcesses { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto start all bots.
        /// </summary>
        /// <value>
        /// <c>true</c> to make the bots start on program load; otherwise,
        /// <c>false</c> to not start them.
        /// </value>
        public bool AutoStartAllBots { get; set; }

        #endregion Top-level config properties

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var fields = this.GetType().GetProperties();

            foreach (var propInfo in fields)
            {
                sb.AppendFormat("{0} = {1}" + Environment.NewLine,
                    propInfo.Name,
                    propInfo.GetValue(this, null));
            }

            return sb.ToString();
        }

        public class BotInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string DisplayName { get; set; }
            public string ChatResponse { get; set; }
            public string LogFile { get; set; }
            public string BotControlClass { get; set; }
            public int MaximumTradeTime { get; set; }
            public int MaximumActionGap { get; set; }
            public string DisplayNamePrefix { get; set; }
            public int TradePollingInterval { get; set; }
            public string LogLevel { get; set; }
            public ulong[] Admins { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to auto start this bot.
            /// </summary>
            /// <value>
            /// <c>true</c> to make the bot start on program load.
            /// </value>
            /// <remarks>
            /// If <see cref="SteamBot.Configuration.AutoStartAllBots "/> is true,
            /// then this property has no effect and is ignored.
            /// </remarks>
            [JsonProperty (Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
            [DefaultValue (true)]
            public bool AutoStart { get; set; }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                var fields = this.GetType().GetProperties();

                foreach (var propInfo in fields)
                {
                    sb.AppendFormat("{0} = {1}" + Environment.NewLine,
                        propInfo.Name, 
                        propInfo.GetValue(this, null));
                }

                return sb.ToString();
            }
        }
    }
}
