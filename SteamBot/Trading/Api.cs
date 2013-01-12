using System;
using System.Text;
using SteamKit2;
using Newtonsoft.Json;
using System.Collections.Specialized;

namespace SteamBot.Trading
{
    public class Api
    {

        public Web web { get; set; }
        public BotHandler botHandler { get; set; }
        public SteamID otherSID { get; set; }

        private string sessionId;
        private string steamLogin;
        private string baseTradeUri;
        private int logPos { get; set; }
        private int version { get; set; }

        static string SteamTradeUri = "/trade/{0}";
        static string SteamCommunityDomain = "steamcommunity.com";

        public Api(SteamID OtherSID, BotHandler botHandler)
        {
            this.otherSID = OtherSID;
            this.botHandler = botHandler;
            this.web = botHandler.web;

            sessionId = botHandler.steamID;
            steamLogin = botHandler.steamLogin;
            baseTradeUri = String.Format(SteamTradeUri, otherSID.ToString());
        }

        public Status GetStatus()
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("sessionid", sessionId);
            data.Add("logpos", "" + logPos);
            data.Add("version", "" + version);

            string result = web.Do(baseTradeUri + "tradestatus", "POST", data);
            return JsonConvert.DeserializeObject<Status>(result);
        }

        #region JSON Responses
        public class Status
        {

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("newversion")]
            public bool NewVersion { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("trade_status")]
            public long TradeStatus { get; set; }

            [JsonProperty("version")]
            public int Version { get; set; }

            [JsonProperty("logpos")]
            public int Logpos { get; set; }

            [JsonProperty("me")]
            public TradeUser Bot { get; set; }

            [JsonProperty("them")]
            public TradeUser Other { get; set; }

        }

        public class TradeUser 
        {

            [JsonProperty("ready")]
            public int Ready { get; set; }

            [JsonProperty("confirmed")]
            public int Confirmed { get; set; }

            [JsonProperty("sec_since_touch")]
            public int SecondsSinceTouch { get; set; }

        }

        public class TradeEvent
        {

            [JsonProperty("steamid")]
            public string SteamId { get; set; }

            [JsonProperty("action")]
            public int Action { get; set; }

            [JsonProperty("timestamp")]
            public ulong Timestamp { get; set; }

            [JsonProperty("appid")]
            public int AppId { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("contextid")]
            public int ContextId { get; set; }

            [JsonProperty("assetid")]
            public ulong AssetId { get; set; }

        }
        #endregion
    }
}
