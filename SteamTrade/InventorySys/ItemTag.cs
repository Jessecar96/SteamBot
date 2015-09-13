using System;

namespace SteamTrade.InventorySys
{
    public sealed class ItemTag : IEquatable<ItemTag>
    {
        public string InternalName { get; internal set; }

        public string Name { get; internal set; }

        public string Category { get; internal set; }

        public string CategoryName { get; internal set; }

        #region Overrides
        public override int GetHashCode() => InternalName.GetHashCode() * Name.GetHashCode() * Category.GetHashCode() * CategoryName.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else if (obj.GetType() != typeof(ItemTag))
                return false;
            ItemTag other = (ItemTag)obj;
            return InternalName == other.InternalName &&
                Name == other.Name &&
                Category == other.Category &&
                CategoryName == other.CategoryName;
        }
        #endregion

        #region Implementation of IEquatable<ItemTag>
        public bool Equals(ItemTag other) => Equals((object)other);
        #endregion

        #region Equality operators
        public static bool operator ==(ItemTag itemTag1, ItemTag itemTag2)
        {
            if (ReferenceEquals(itemTag1, itemTag2))
                return true;
            else if ((object)itemTag1 == null || (object)itemTag2 == null)
                return false;
            else
                return itemTag1.Equals(itemTag2);
        }

        public static bool operator !=(ItemTag itemTag1, ItemTag itemTag2) => !(itemTag1 == itemTag2);
        #endregion
    }
}
