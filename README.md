## Info ##

SteamBot is a bot for interacting with Steam Chat and Trading.

## Configuration Instructions ##

### Step 1 ###
First, you need to configure your bots.  
Edit the file settings.json in \SteamBot\\bin\Debug  
Put your API key in there with the bots usernames and passwords  
You can run multiple bots at the same time by having multiple elements in the "Bots" array.  

### Step 2 ###
Next you need to actually edit the bot to make it do what you want.  
You mainly only need to edit the file TradeEnterTradeListener.cs  
It contains events for everything you need.    
Just add your code to each of the events.  It explains what each of them do in the code comments.  
Look at Usage below to see some usefull functions.  

## Usage ##
**Here some useful functions you can use in TradeEnterTradeListener:**  
trade - The master class referring back to the current trade.  
trade.AddItem(ulong itemid, int slot) - Add an item by its "id" property into the specified slot in the trade.  
trade.AddItemByDefindex(int defindex, int slot) - Same as AddItem, but you specify the defindex of the item instead of the id.  
trade.RemoveItem(ulong itemid, int slot) - Removes the specified item from the trade.  
trade.SetReady(bool ready) - Sets the trade ready or not ready according to the boolean.  
trade.AcceptTrade() - Accepts the trade.  
trade.SendMessage(string msg) - Sends a message to the other user over trade chat.  

## More help? ##
You can contact me at http://steamcommunity.com/id/jessecar  
But please, have some programming knowledge.  
Don't come to me asking how to make a huge complicated bot when you don't know anything.  