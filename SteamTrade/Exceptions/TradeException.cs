﻿using System;

namespace SteamTrade.Exceptions
{
    /// <summary>
    /// A basic exception that occurs in the trading library.
    /// </summary>
    public class TradeException : Exception
    {
        public TradeException()
        {
        }

        public TradeException(string message)
            : base(message)
        {
        }

        public TradeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
