## Info ##

**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 6 contributors have all added to the bot.  The bot is publicly available, and is available under the LGPL license (we plan on moving towards the MIT license later on).

## Configuration Instructions ##

### Step 1 ###
1. First, you need to configure your bots.  
2. Edit the file `settings.json` in `\SteamBot\bin\Debug`.
3. **Put your API key in there with the bots usernames and passwords** - This is important, as the bot will not work without it.
4. You can run multiple bots at the same time by having multiple elements in the `Bots` array.  

### Step 2 ###
1. Next you need to actually edit the bot to make it do what you want.  
2. You mainly only need to edit the file `TradeEnterTradeListener.cs`, as it contains events for everything you need.    
3. Just add your code to each of the events.  It explains what each of them do in the code comments.  
4. Look at Usage below to see some usefull functions.  

## Usage ##
**Here some useful functions you can use in TradeEnterTradeListener:**  
### `trade` ###
The master class referring back to the current trade.  
### `trade.AddItem(ulong itemid, int slot)` ###
Add an item by its `id` property into the specified slot in the trade.  
### `trade.AddItemByDefindex(int defindex, int slot)` ### 
Same as AddItem, but you specify the defindex of the item instead of the id.  
### `trade.RemoveItem(ulong itemid, int slot)` ###
Removes the specified item from the trade.  
### `trade.SetReady(bool ready)` ###
Sets the trade ready or not ready according to the boolean.  
### `trade.AcceptTrade()` ###
Accepts the trade.  
### `trade.SendMessage(string msg)` ###
ends a message to the other user over trade chat.  

## More help? ##
If it's a bug, open an Issue; if you have a fix, open a Pull Request.  If you have questions or comments, contact <http://steamcommunity.com/jessecar>.