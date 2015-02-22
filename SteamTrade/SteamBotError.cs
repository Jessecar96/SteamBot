using System;

namespace SteamTrade
{
    public class SteamBotError
    {
        public SteamBotErrorType type { get; private set; }
        public string message { get; private set; }

        public SteamBotError (string message, SteamBotErrorType type = SteamBotErrorType.DEFAULT)
        {
            this.type = type;
            this.message = message;
        }

        public enum SteamBotErrorType
        {
            DEFAULT,
            INVENTORY_PRIVATE,
            TRADE_CANCELED_BY_USER,
            TRADE_SESSION_EXPIRED,
            UNKNOWN
        }
    }
}
