using System;
using System.Collections.Generic;

namespace SteamBot
{
	public class UserInventory
	{
		
		public bool success{get;set;}

		public rgInv rgInventory{get;set;}
	}
	
	public class rgInv
	{
		
	}
	
	public class rgItems
	{
		
		public long id{get;set;}
		
		public long classid{get;set;}
		
		public long instanceid{get;set;}
		
		public int amount{get;set;}
		
		public int pos{get;set;}	
		
	}
}

