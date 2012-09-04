using System;
using System.Collections.Generic;

namespace SteamBot
{
	public class PlayerInventory
	{
		public InventoryResult result{ get; set; }
        public InventoryItem GetItem(ulong id)
        {
            if (result == null) return null;
            foreach(InventoryItem item in result.items)
            {
                if (item.id == id.ToString())
                    return item;
            }
            return null;
        }
	}
	
	public class InventoryResult
	{
		public string status{ get; set; }

		public string num_backpack_slots{ get; set; }

		public InventoryItem[] items{ get; set; }

	}

	public class InventoryItem
	{
		
		public string id{get;set;}

		public string original_id{ get; set; }

		public string defindex{get;set;}
		
		public string level{get;set;}
		
		public string quality{get;set;}
		
		public string pos{get;set;}

	    public bool flag_cannot_craft { get; set; }

	    public ItemAttributes[] attributes{get;set;}

        public SchemaItem GetSchemaItem(ItemSchema schema)
        {
            return schema.GetItem(defindex);
        }
		
	}

	public class ItemAttributes
	{
		public string defindex{get;set;}

		public string value{get;set;}
	}


	public class ItemSchema
	{
		public SchemaResult result{get;set;}

        public SchemaItem GetItem(string defindex)
        {
            if(result == null) return null;
            foreach(SchemaItem item in result.items)
            {
                if(item.defindex == defindex)
                    return item;
            }
            return null;
        }
	}

	public class SchemaResult
	{
		public string status{get;set;}

		public string items_game_url{ get; set; }

		public SchemaItem[] items{get;set;}

        public ItemOrigin[] originNames { get; set; }
	}

    public class ItemOrigin
    {
        public int origin { get; set; }
        public string name { get; set; }
    }

	public class SchemaItem
	{
		public string name { get; set; }

		public string defindex{ get; set; }

		public string item_class { get; set; }

		public string item_type_name { get; set; }

		public string item_name { get; set; }

		public string craft_material_type { get; set; }

        public string[] used_by_classes { get; set; }
	}


    public class TradeLog
    {
        public TradeLog()
        {
            ItemsReceived = new List<TradeItem>();
            ItemsLost = new List<TradeItem>();
        }

        public List<TradeItem> ItemsReceived { get; set; }
        public List<TradeItem> ItemsLost { get; set; }
    }

    public class TradeItem
    {
        public ulong ItemID { get; set; }
        public string DefIndex { get; set; }
        public string Quality { get; set; }
    }
}

