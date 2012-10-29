## Info ##

**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 6 contributors have all added to the bot.  The bot is publicly available, and is available under the MIT License.

## Configuration Instructions ##

### Step 0 ###
If you've recently just cloned this repository, there are a few things you need to do.

1. Run `git submodule init` in order to initalize the submodule configuration file.
2. Run `git submodule update` to pull the latest version of the submodules that are included (namely, SteamKit2).
3. Build the program.  Since SteamKit2 is licensed under the LGPL, and SteamBot should be released under the MIT license, SteamKit2's code cannot be included in SteamBot.  This includes executables.  We'll probably make downloads available on github.
4. Continue on like normal.

### Step 1 ###
1. First, you need to configure your bots.
2. Edit the file `settings-template.json` in `\SteamBot\bin\Debug`.  Some configuration options:

   - `Admins`: An array of Steam Profile IDs of the users that are an Admin of your bot(s). Each Profile ID should be a string enclosed in quotes and seperated by a comma. These admins are global to all bots listed in the _Bots_ array.
   - `ApiKey`: The API key you have been assigned by Valve. If you do not have one, it can be requested from Value at their [Web API Key](http://steamcommunity.com/dev/apikey) page. **This is required and the bot(s) will not work without an API Key**. The API Key should be a string enclosed by quotes.
   - `mainLog`: The log containing runtime information for all bots.
   - `Bots`: An array of dictionaries containing information about each individual bot you will be running. You can run multiple bots at the same time by having multiple elements in the `Bots` array. Each entry in the `Bots` array consists of the following values.
    - `Username`: The Steam user name for this bot. It should be a string enclosed by quotes.
    - `Password`: The password for the Steam user associated with this bot. It should be a string enclosed by quotes.
    - `DisplayName`: The name the bot will present on Steam. It should be a string enclosed by quotes.
    - `ChatResponse`: This is the response the bot will provide when a user chats with it via Steam Friends. It should be a string enclosed by quotes.
    - `logFile`: The log file for this specific bot. It should be a string encluded by quotes.
    - `Admins`: Additional admins, specific to this bot. _(optional)_
    - `MaximumTradeTime`: Maximium length of time for a trade session (in seconds). It should be a numeric value. Defaults to 180 seconds. _(optional)_
    - `MaximumActionGap`: Length of time the bot will allow the user to remain inactive. It should be a numeric value. Defaults to 30 seconds. _(optional)_
    - `DisplayNamePrefix`: A prefix to display in front of the DisplayName. It should be a string encloded by quotes. Defaults to an empty string. _(optional)_
    - `TradePollingInterval`: Length of time, in milliseconds, between polling events. Higher values reduce CPU usage at the cost of a slower trading session. It should be a numeric value. Default is 800 ms. Lowest value is 100 ms. _(optional)_

3. Rename `settings-template.json` to `settings.json`
 
### Step 2 ###
1. Next you need to actually edit the bot to make it do what you want.
2. You mainly only need to edit the file `TradeEnterTradeListener.cs`, as it contains events for everything you need.
3. Just add your code to each of the events.  It explains what each of them do in the code comments.
4. Look at Usage below to see some usefull functions.

## Usage ##
Here some useful functions you can use in TradeEnterTradeListener:
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
If it's a bug, open an Issue; if you have a fix, open a Pull Request.  A list of contributors (add yourself if you want to):
- [Jessecar96](http://steamcommunity.com/id/jessecar) (project lead)
- [geel9](http://steamcommunity.com/id/geel9)
- [Dr. Cat, MD or redjazz96](http://steamcommunity.com/id/redjazz96)

SteamBot is licensed under the MIT license.  Check out LICENSE for more details.

## Wanna Contribute? ##
Check out CONTRIBUTING.md.
