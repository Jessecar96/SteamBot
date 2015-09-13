using System;
using System.Collections.Generic;
using System.Reflection;

namespace SteamTrade.InventorySys
{
    public sealed class InventoryType :
        IEquatable<InventoryType>,
        IEquatable<Game>,
        IEquatable<uint>
    {
        #region Supported Inventories
        public static readonly InventoryType TeamFortress2 = new InventoryType(Game.TeamFortress2, 2);
        public static readonly InventoryType Dota2 = new InventoryType(Game.Dota2, 2);
        public static readonly InventoryType Portal2 = new InventoryType(Game.Portal2, 2);
        public static readonly InventoryType CSGO = new InventoryType(Game.CSGO, 2);
        public static readonly InventoryType Steam_Gifts = new InventoryType(Game.Steam, 1);
        public static readonly InventoryType Steam_Coupons = new InventoryType(Game.Steam, 3);
        public static readonly InventoryType Steam_Community = new InventoryType(Game.Steam, 6);
        public static readonly InventoryType Steam_ItemRewards = new InventoryType(Game.Steam, 7);
        public static readonly InventoryType Warframe = new InventoryType(Game.Warframe, 2);
        public static readonly InventoryType BattleBlockTheater = new InventoryType(Game.BattleBlockTheater, 2);
        public static readonly InventoryType PathOfExile = new InventoryType(Game.PathOfExile, 1);
        public static readonly InventoryType SinsOfDarkAge = new InventoryType(Game.SinsOfDarkAge, 1);
        public static readonly InventoryType Rust = new InventoryType(Game.Rust, 2);
        public static readonly InventoryType RobotRoller_DerbyDiscoDodgeball = new InventoryType(Game.RobotRoller_DerbyDiscoDodgeball, 2);
        public static readonly InventoryType H1Z1 = new InventoryType(Game.H1Z1, 1);
        public static readonly InventoryType Altitude0_LowerAndFaster = new InventoryType(Game.Altitude0_LowerAndFaster, 1);
        public static readonly InventoryType PrimalCarnage_Extinction = new InventoryType(Game.PrimalCarnage_Extinction, 1);
        public static readonly InventoryType RatzInstagib = new InventoryType(Game.RatzInstagib, 2);
        #endregion

        internal static IEnumerable<InventoryType> SupportedInventories
        {
            get
            {
                List<InventoryType> supportedInventories = new List<InventoryType>();
                foreach (FieldInfo info in typeof(InventoryType).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    InventoryType type = info.GetValue(null) as InventoryType;
                    if (type != null)
                        supportedInventories.Add(type);
                }
                return supportedInventories;
            }
        }

        public readonly Game Game;

        public readonly long ContextId;

        private InventoryType(Game game, long contextId)
        {
            Game = game;
            ContextId = contextId;
        }

        #region Overrides
        public override int GetHashCode() => Game.GetHashCode() * ContextId.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            InventoryType other = obj as InventoryType;
            if ((object)other == null)
                return false;
            return Game == other.Game && ContextId == other.ContextId;
        }

        public override string ToString() => $"Game: {Game}; Subtype: {ContextId}";
        #endregion

        #region Implementations of IEquatable<>
        public bool Equals(InventoryType other) => Equals((object)other);

        public bool Equals(Game other) => Game == other;

        public bool Equals(uint other) => (uint)Game == other;
        #endregion

        #region Equality operators
        public static bool operator ==(InventoryType inventoryType1, InventoryType inventoryType2)
        {
            if (ReferenceEquals(inventoryType1, inventoryType2))
                return true;
            else if ((object)inventoryType1 == null || (object)inventoryType2 == null)
                return false;
            else
                return inventoryType1.Equals(inventoryType2);
        }

        public static bool operator !=(InventoryType inventoryType1, InventoryType inventoryType2) => !(inventoryType1 == inventoryType2);

        public static bool operator ==(InventoryType inventoryType, Game game)
        {
            if (inventoryType == null)
                return false;
            else
                return inventoryType.Equals(game);
        }

        public static bool operator !=(InventoryType inventoryType, Game game) => !(inventoryType == game);

        public static bool operator ==(Game game, InventoryType inventoryType) => inventoryType == game;

        public static bool operator !=(Game game, InventoryType inventoryType) => !(game == inventoryType);

        public static bool operator ==(InventoryType inventoryType, uint game)
        {
            if (inventoryType == null)
                return false;
            else
                return inventoryType.Equals(game);
        }

        public static bool operator !=(InventoryType inventoryType, uint game) => !(inventoryType == game);

        public static bool operator ==(uint game, InventoryType inventoryType) => inventoryType == game;

        public static bool operator !=(uint game, InventoryType inventoryType) => !(game == inventoryType);
        #endregion

        #region Conversion operators
        public static explicit operator Game(InventoryType inventoryType) => inventoryType.Game;

        public static explicit operator uint(InventoryType inventoryType) => (uint)inventoryType.Game;
        #endregion
    }
}
