using System;

namespace SteamTrade.InventorySys
{
    public sealed class ItemAppData : IEquatable<ItemAppData>
    {
        #region Steam community
        public uint AppId { get; internal set; }

        public uint ItemType { get; internal set; }

        public uint Denomination { get; internal set; }
        #endregion

        #region TF2
        public uint DefIndex { get; internal set; }

        public uint Quality { get; internal set; }
        #endregion

        #region Overrides
        public override int GetHashCode()
        {
            int hash = 1;
            if (AppId > 0)
                hash *= AppId.GetHashCode();
            if (ItemType > 0)
                hash *= ItemType.GetHashCode();
            if (Denomination > 0)
                hash *= Denomination.GetHashCode();
            if (DefIndex > 0)
                hash *= DefIndex.GetHashCode();
            if (Quality > 0)
                hash *= Quality.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else if (obj.GetType() != typeof(ItemAppData))
                return false;
            ItemAppData other = (ItemAppData)obj;
            return AppId == other.AppId &&
                ItemType == other.ItemType &&
                Denomination == other.Denomination &&
                DefIndex == other.DefIndex &&
                Quality == other.Quality;
        }
        #endregion

        #region Implementation of IEquatable<ItemAppData>
        public bool Equals(ItemAppData other) => Equals((object)other);
        #endregion

        #region Equality operators
        public static bool operator ==(ItemAppData itemAppData1, ItemAppData itemAppData2)
        {
            if (ReferenceEquals(itemAppData1, itemAppData2))
                return true;
            else if ((object)itemAppData1 == null || (object)itemAppData2 == null)
                return false;
            else
                return itemAppData1.Equals(itemAppData2);
        }

        public static bool operator !=(ItemAppData itemAppData1, ItemAppData itemAppData2) => !(itemAppData1 == itemAppData2);
        #endregion
    }
}
