using System;
using SteamKit2;

namespace SteamTrade
{
    public class TradeManager
    {
        const int MaxGapTimeDefault = 30;
        const int MaxTradeTimeDefault = 180;

        string apiKey;
        string sessionId;
        string token;

        public TradeManager (string apiKey, string sessionId, string token)
        {
            if (apiKey == null)
                throw new ArgumentNullException ("apiKey");

            if (sessionId == null)
                throw new ArgumentNullException ("sessionId");

            if (token == null)
                throw new ArgumentNullException ("token");

            SetTradeTimeLimits(MaxTradeTimeDefault, MaxGapTimeDefault);

            this.apiKey = apiKey;
            this.sessionId = sessionId;
            this.token = token;
        }

        #region Public Properties

        public int MaxTradeTimeSec
        {
            get; 
            private set;
        }

        public int MaxActionGapSec
        {
            get;
            private set;
        }

        #endregion Public Properties

        /// <summary>
        /// Sets the trade time limits.
        /// </summary>
        /// <param name='maxTradeTime'>
        /// Max trade time in seconds.
        /// </param>
        /// <param name='maxActionGap'>
        /// Max gap between user action in seconds.
        /// </param>
        public void SetTradeTimeLimits(int maxTradeTime, int maxActionGap)
        {
            MaxTradeTimeSec = maxTradeTime;
            MaxActionGapSec = maxActionGap;
        }

        public Trade StartTrade (SteamID  me, SteamID other)
        {
            var t = new Trade (me, other, sessionId, token, apiKey, MaxTradeTimeSec, MaxActionGapSec);

            return t;
        }
    }
}

