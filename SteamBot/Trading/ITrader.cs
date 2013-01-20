using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.Trading
{
    public interface ITrader
    {
        /// <summary>
        /// Called whenever the status of the server is updated.  This can 
        /// happen as a part of the poll, or whenever an action takes place.
        /// This may or may not be called in its own thread.
        /// </summary>
        /// <param name="status">The current status of the server.</param>
        void OnStatusUpdate(Api.Status status);

        /// <summary>
        /// This is called to initalize the values.
        /// </summary>
        void Start();

        Trade trade { get; set; }
    }
}
