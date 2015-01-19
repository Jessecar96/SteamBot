using System;

namespace SteamTrade
{
    public class SteamBotError
    {
        SteamBotErrorType type;
        string message;

        public SteamBotError (string message, SteamBotErrorType type = SteamBotErrorType.DEFAULT)
        {
            this.type = type;
            this.message = message;
        }

        public SteamBotErrorType getErrorType(){
            return type;
        }

        public string getMessage(){
            return message;
        }

        public enum SteamBotErrorType{
			DEFAULT,
            INVENTORY_PRIVATE,
            TRADE_CANCELED_BY_USER,
			TRADE_SESSION_EXPIRED,
            UNKNOWN
        }
    }
}

