using System;

namespace SteamKit2
{
    /// <summary>
    /// The status of a trade request.
    /// </summary>
    public enum ETradeStatus
    {
        Accepted = 0,
        Rejected = 1,
        Cancelled = 7,
        Unknown = 8,
        InTrade = 11,
        Unknown2 = 12,
        TimedOut = 13,
    }

    public partial class SteamTrading
    {
        /// <summary>
        /// This callback is fired in response to receiving the result of a trade request.
        /// </summary>
        public sealed class TradeRequestCallback : CallbackMsg
        {
            /// <summary>
            /// Gets the response of the request.
            /// </summary>
            public UInt32 Response;

            /// <summary>
            /// Gets the id of the trade request.
            /// </summary>
            public UInt32 TradeRequestId;

            /// <summary>
            /// Gets the SteamID of the other trader.
            /// </summary>
            public SteamID Other;

            /// <summary>
            /// Gets the status of the trade.
            /// </summary>
            public ETradeStatus Status;
        }

        /// <summary>
        /// This callback is fired in response to receiving a trade proposal.
        /// </summary>
        public sealed class TradeProposedCallback : CallbackMsg
        {
            /// <summary>
            /// Gets the id of the trade request.
            /// </summary>
            public UInt32 TradeRequestId;

            /// <summary>
            /// Gets the SteamID of the other trader.
            /// </summary>
            public SteamID Other;
        }

        public sealed class TradeCancelRequestCallback : CallbackMsg
        {
            /// <summary>
            /// Gets the SteamID of the other trader.
            /// </summary>
            public SteamID Other;
        }

        public sealed class TradeStartSessionCallback : CallbackMsg
        {
            /// <summary>
            /// Gets the SteamID of the other trader.
            /// </summary>
            public SteamID Other;
        }
    }
}
