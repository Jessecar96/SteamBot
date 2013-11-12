using System;
using System.Globalization;

namespace SteamTrade
{
    /// <summary>
    /// This represents an inventory as decoded from the Steam Trade API 
    /// function of the same name.
    /// </summary>
    /// <remarks>
    /// This class takes the results of the following Trade API call:
    /// POST /trade/(steamid)/foreigninventory/sessionid=(trade_session_id)&steamid=(steamid)&appid=(appid)&contextid=(trade contextid)
    /// 
    /// The trade context id is important and only obtainable from being in
    /// a trade.
    /// </remarks>
    public class ForeignInventory
    {
        private readonly dynamic rawJson;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignInventory"/> class.
        /// </summary>
        /// <param name="rawJson">
        /// The json returned from the foreigninventory Web API call.
        /// </param>
        public ForeignInventory (dynamic rawJson)
        {
            this.rawJson = rawJson;

            if (rawJson.success == "true")
            {
                InventoryValid = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the inventory is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the inventory is valid; otherwise, <c>false</c>.
        /// </value>
        public bool InventoryValid
        {
            get; private set;
        }

        /// <summary>
        /// Gets the class id for the given item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>A class ID or 0 if there is an error.</returns>
        public uint GetClassIdForItemId(ulong itemId)
        {
            string i = itemId.ToString(CultureInfo.InvariantCulture);

            try
            {
                return rawJson.rgInventory[i].classid;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;    
            }
        }

        /// <summary>
        /// Gets the instance id for given item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>A instance ID or 0 if there is an error.</returns>
        public ulong GetInstanceIdForItemId(ulong itemId)
        {
            string i = itemId.ToString(CultureInfo.InvariantCulture);

            try
            {
                return rawJson.rgInventory[i].instanceid;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }


        /// <summary>
        /// Gets the defindex for a given Item ID.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>A defindex or 0 if there is an error.</returns>
        public ushort GetDefIndex(ulong itemId)
        {
            uint classId = GetClassIdForItemId(itemId);
            ulong iid = GetInstanceIdForItemId(itemId);

            string index = classId + "_" + iid;

            string r;

            try
            {
                // for tf2 the def index is in the app_data section in the 
                // descriptions object. this may not be the case for all
                // games and therefore this may be non-portable.
                r = rawJson.rgDescriptions[index].app_data.def_index;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }

            return ushort.Parse(r);
        }
    }
}
