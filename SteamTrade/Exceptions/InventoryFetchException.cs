using System;
using SteamKit2;

namespace SteamTrade.Exceptions
{
    public class InventoryFetchException : TradeException
    {
        public InventoryFetchException ()
        {
        }

        /// <summary>
        /// Gets the Steam identifier that caused the fetch exception.
        /// </summary>
        /// <value>
        /// The failing steam identifier.
        /// </value>
        public SteamID FailingSteamId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SteamTrade.Exceptions.InventoryFetchException"/> class.
        /// </summary>
        /// <param name='steamId'>
        /// Steam identifier that caused the fetch exception.
        /// </param>
        public InventoryFetchException (SteamID steamId)
            : base(String.Format("Failed to fetch inventory for: {0}", steamId.ToString()))
        {
            FailingSteamId = steamId;
        }
    }
}

