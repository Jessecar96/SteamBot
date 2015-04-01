using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamBot
{
    public class Configuration
    {
        private class JsonToSteamID : JsonConverter
        {
            static Regex Steam2Regex = new Regex(
               @"STEAM_(?<universe>[0-5]):(?<authserver>[0-1]):(?<accountid>\d+)",
               RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(IEnumerable<SteamKit2.SteamID>);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JArray array = JArray.Load(reader);
                List<SteamKit2.SteamID> ret = new List<SteamKit2.SteamID>();
                foreach (JToken id in array)
                {
                    string sID = (string)id;
                    if (Steam2Regex.IsMatch(sID))
                        ret.Add(new SteamKit2.SteamID(sID));
                    else
                        ret.Add(new SteamKit2.SteamID((ulong)id));
                }
                return ret;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public static Configuration LoadConfiguration (string filename)
        {
            TextReader reader = new StreamReader(filename);
            string json = reader.ReadToEnd();
            reader.Close();

            Configuration config =  JsonConvert.DeserializeObject<Configuration>(json);

            config.Admins = config.Admins ?? new SteamKit2.SteamID[0];

            // merge bot-specific admins with global admins
            foreach (BotInfo bot in config.Bots)
            {
                if (bot.Admins == null)
                    bot.Admins = config.Admins;
                else
                    bot.Admins = bot.Admins.Concat(config.Admins);
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
        [JsonConverter(typeof(JsonToSteamID))]
        public IEnumerable<SteamKit2.SteamID> Admins { get; set; }

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
            public string ApiKey { get; set; }
            public string DisplayName { get; set; }
            public string ChatResponse { get; set; }
            public string LogFile { get; set; }
            public string BotControlClass { get; set; }
            public int MaximumTradeTime { get; set; }
            public int MaximumActionGap { get; set; }
            public string DisplayNamePrefix { get; set; }
            public int TradePollingInterval { get; set; }
            public string ConsoleLogLevel { get; set; }
            public string FileLogLevel { get; set; }
            [JsonConverter(typeof(JsonToSteamID))]
            public IEnumerable<SteamKit2.SteamID> Admins { get; set; }
            public string SchemaLang { get; set; }

            // Depreciated configuration options
            public string LogLevel { get; set; }

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
