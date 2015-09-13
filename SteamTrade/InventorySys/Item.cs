using System;
using System.Collections.Generic;

namespace SteamTrade.InventorySys
{
    public sealed class Item :
        IEquatable<Item>,
        IEquatable<InventoryType>,
        IEquatable<Game>,
        IEquatable<uint>,
        IEquatable<ulong>
    {
        public InventoryType InventoryType { get; internal set; }

        public ulong Id { get; internal set; }

        public string Name { get; internal set; }

        public string OriginalName { get; internal set; }

        public string Type { get; internal set; }

        public bool IsTradable { get; internal set; }

        public bool IsCommodity { get; internal set; }

        public IEnumerable<string> Descriptions { get; internal set; }

        public IEnumerable<ItemTag> Tags { get; internal set; }

        public ItemAppData AppData { get; internal set; }

        #region Overrides
        public override int GetHashCode() => InventoryType.GetHashCode() *
            Id.GetHashCode() *
            Name.GetHashCode() *
            OriginalName.GetHashCode() *
            Type.GetHashCode() *
            IsTradable.GetHashCode() *
            IsCommodity.GetHashCode() *
            (Descriptions != null ? Descriptions.GetHashCode<string>() : 1) *
            (Tags != null ? Tags.GetHashCode<ItemTag>() : 1) *
            (AppData != null ? AppData.GetHashCode() : 1);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Item other = obj as Item;
            if ((object)other == null)
                return false;
            return InventoryType == other.InventoryType &&
                Id == other.Id &&
                Name == other.Name &&
                OriginalName == other.OriginalName &&
                Type == other.Type &&
                IsTradable == other.IsTradable &&
                IsCommodity == other.IsCommodity &&
                (Descriptions == null ? Descriptions == other.Descriptions : Descriptions.Equals<string>(other.Descriptions)) &&
                (Tags == null ? Tags == other.Tags : Tags.Equals<ItemTag>(other.Tags)) &&
                AppData == other.AppData;
        }
        #endregion

        #region Implementations of IEquatable<>
        public bool Equals(Item other) => Equals((object)other);

        public bool Equals(InventoryType other) => InventoryType == other;

        public bool Equals(Game other) => InventoryType == other;

        public bool Equals(uint other) => (uint)InventoryType == other;

        public bool Equals(ulong other) => Id == other;
        #endregion

        #region Equality operators
        public static bool operator ==(Item item1, Item item2)
        {
            if (ReferenceEquals(item1, item2))
                return true;
            else if ((object)item1 == null || (object)item2 == null)
                return false;
            else
                return item1.Equals(item2);
        }

        public static bool operator !=(Item item1, Item item2) => !(item1 == item2);

        public static bool operator ==(Item item, InventoryType inventoryType)
        {
            if ((object)item == null && inventoryType == null)
                return true;
            else if ((object)item == null || inventoryType == null)
                return false;
            else
                return item.Equals(inventoryType);
        }

        public static bool operator !=(Item item, InventoryType inventoryType) => !(item == inventoryType);

        public static bool operator ==(InventoryType inventoryType, Item item) => item == inventoryType;

        public static bool operator !=(InventoryType inventoryType, Item item) => !(inventoryType == item);

        public static bool operator ==(Item item, Game game)
        {
            if ((object)item == null)
                return false;
            else
                return item.Equals(game);
        }

        public static bool operator !=(Item item, Game game) => !(item == game);

        public static bool operator ==(Game game, Item item) => item == game;

        public static bool operator !=(Game game, Item item) => !(game == item);

        public static bool operator ==(Item item, uint game)
        {
            if ((object)item == null)
                return false;
            else
                return item.Equals(game);
        }

        public static bool operator !=(Item item, uint game) => !(item == game);

        public static bool operator ==(uint game, Item item) => item == game;

        public static bool operator !=(uint game, Item item) => !(game == item);
        #endregion

        #region Conversion operator
        public static explicit operator ulong (Item item) => item.Id;
        #endregion
    }
}
