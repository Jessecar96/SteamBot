using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SteamTrade.TradeWebAPI
{
    public class TradeStatus
    {
        public string error { get; set; }
            
        public bool newversion { get; set; }
            
        public bool success { get; set; }
            
        public long trade_status { get; set; }
            
        public int version { get; set; }
            
        public int logpos { get; set; }
            
        public TradeUserObj me { get; set; }
            
        public TradeUserObj them { get; set; }
            
        public TradeEvent[] events { get; set; }

        public TradeEvent GetLastEvent()
        {
            if (events == null || events.Length == 0)
                return null;

            return events[events.Length - 1];
        }

        public TradeEvent[] GetAllEvents()
        {
            if (events == null)
                return new TradeEvent[0];

            return events;
        }
    }

    public class TradeEvent : UserAsset
    {
        public ulong assetid { get { return Id; } set { Id = value; } }
        public string steamid { get; set; }

        public int action { get; set; }

        public ulong timestamp { get; set; }

        public string text { get; set; }

        public override int GetHashCode()
        {
            return string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}", 
                Id, AppId, ContextId, Amount, steamid, action, timestamp).GetHashCode();
        }

        /// <summary>
        /// Determins if the TradeEvent is equal to another.
        /// </summary>
        /// <param name="other">TradeEvent to compare to</param>
        /// <returns>True if equal, false if not</returns>
        /// 
        public override bool Equals(object obj)
        {
            if (!(obj is TradeEvent))
                return false;

            TradeEvent other = (TradeEvent)obj;

            return this.steamid == other.steamid && this.action == other.action
                   && this.timestamp == other.timestamp && this.AppId == other.AppId
                   && this.text == other.text && this.ContextId == other.ContextId
                   && this.Id == other.Id;
        }
    }

    public class TradeUserObj
    {
        public int ready { get; set; }

        public int confirmed { get; set; }

        public int sec_since_touch { get; set; }

        public dynamic assets { get; set; }

        /// <summary>
        /// Gets the array from the TradeUserObj. Use this instead 
        /// of the <see cref="assets"/> property directly.
        /// </summary>
        /// <returns>An array of <see cref="TradeUserAssets"/></returns>
        public UserAsset[] GetAssets()
        {
            var tradeUserAssetses = new List<UserAsset>();

            // if items were added in trade the type is an array like so:
            // a normal JSON array
            // "assets": [
            //    {
            //        "assetid": "1693638354", <snip>
            //    }
            //],
            if (assets.GetType() == typeof(JArray))
            {
                foreach (var asset in assets)
                {
                    tradeUserAssetses.Add(new UserAsset()
                    {
                        Amount = asset.amount,
                        AppId = asset.appid,
                        Id = asset.assetid,
                        ContextId = asset.contextid
                    });
                }
            }
            else if (assets.GetType() == typeof(JObject))
            {
                // when items are removed from trade they look like this:
                // a JSON object like a "list"
                // (item in trade slot 1 was removed)
                // "assets": {
                //    "2": {
                //        "assetid": "1745718856", <snip>
                //    },
                //    "3": {
                //        "assetid": "1690644335", <snip>
                //    }
                //},
                foreach (JProperty obj in assets)
                {
                    dynamic value = obj.Value;
                    tradeUserAssetses.Add(new UserAsset()
                    {
                        AppId = value.appid,
                        Amount = value.amount,
                        Id = value.assetid,
                        ContextId = value.contextid
                    });
                }
            }

            return tradeUserAssetses.ToArray();
        }
    }

    public enum TradeEventType
    {
        ItemAdded = 0, //itemid = "assetid"
        ItemRemoved = 1, //itemid = "assetid"
        UserSetReady = 2,
        UserSetUnReady = 3,
        UserAccept = 4,
        //5 = ?? Maybe some sort of cancel?
        //6 = ??
        UserChat = 7 //message = "text"
    }
}