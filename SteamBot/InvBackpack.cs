using System;
using System.Collections.Generic;

namespace SteamBot
{
	public class rgInventory
	{
		
		public rgResult result{ get; set; }
	}
	
	public class rgResult
	{
		public string status{ get; set; }

		public string num_backpack_slots{ get; set; }

		public rgItems[] items{ get; set; }

	}

	public class rgItems
	{
		
		public string id{get;set;}

		public string original_id{ get; set; }

		public string defindex{get;set;}
		
		public string level{get;set;}
		
		public string quality{get;set;}
		
		public string pos{get;set;}

		public rgAttributes[] attributes{get;set;}
		
	}

	public class rgAttributes
	{
		public string defindex{get;set;}

		public string value{get;set;}


	}


	public class itemSchema
	{

		public itResult result{get;set;}

	}

	public class itResult
	{
		public string status{get;set;}

		public string items_game_url{ get; set; }

		public itItems[] items{get;set;}

	}

	public class itItems
	{

		public string name { get; set; }

		public string defindex{ get; set; }

		public string item_class { get; set; }

		public string item_type_name { get; set; }

		public string item_name { get; set; }

		public string craft_material_type { get; set; }

	}
}

