## Info ##

**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 8 contributors have all added to the bot.  The bot is publicly available under the MIT License.

## Configuration Instructions ##

### Step 0 ###
If you've just recently cloned this repository, there are a few things you need to do.

1. Run `git submodule init` to initalize the submodule configuration file.
2. Run `git submodule update` to pull the latest version of the submodules that are included (namely, SteamKit2).
3. Build the program.  Since SteamKit2 is licensed under the LGPL, and SteamBot should be released under the MIT license, SteamKit2's code cannot be included in SteamBot.  This includes executables.  We'll probably make downloads available on GitHub.
4. Continue on like normal.

### Step 1 ###
1. First, you need to configure your bots.
2. Edit the file `settings-template.json` in `\SteamBot\bin\Debug`.  Some configuration options:

   - `Admins`: An array of Steam Profile IDs of the users that are an Admin of your bot(s). Each Profile ID should be a string in quotes and seperated by a comma. These admins are global to all bots listed in the `Bots` array.
   - `ApiKey`: The API key you have been assigned by Valve. If you do not have one, it can be requested from Value at their [Web API Key](http://steamcommunity.com/dev/apikey) page. **This is required and the bot(s) will not work without an API Key**. The API Key should be a string in quotes.
   - `mainLog`: The log containing runtime information for all bots.
   - `Bots`: An array of dictionaries containing information about each individual bot you will be running. You can run multiple bots at the same time by having multiple elements in the `Bots` array. Each entry in the `Bots` array consists of the following values.
    - `Username`: The Steam user name for this bot. It should be a string in quotes. **required**
    - `Password`: The password for the Steam user associated with this bot. It should be a string in quotes. **required**
    - `DisplayName`: The name the bot will present on Steam. It should be a string in quotes. **required**
    - `ChatResponse`: This is the response the bot will provide when a user chats with it via Steam Friends. It should be a string in quotes. **required**
    - `logFile`: The log file for this specific bot. It should be a string in quotes. **required**
    - `BotControlClass`: The fully qualified class that controls how this specific bot will behave. Generally, this is a seperate file (ie. `SimpleUserHandler.cs`) and has the same name as your class (without the trailing `.cs` extension). It must be the fully qualified class (ie. `SteamBot.SimpleUserHandler`). It should be a string in quotes. **required**
    - `Admins`: Additional admins, specific to this bot. _(optional)_
    - `MaximumTradeTime`: Maximium length of time for a trade session (in seconds). It should be a numeric value. Defaults to 180 seconds. _(optional)_
    - `MaximumActionGap`: Length of time the bot will allow the user to remain inactive. It should be a numeric value. Defaults to 30 seconds. _(optional)_
    - `DisplayNamePrefix`: A prefix to display in front of the DisplayName. It should be a string encloded by quotes. Defaults to an empty string. _(optional)_
    - `TradePollingInterval`: Length of time, in milliseconds, between polling events. Higher values reduce CPU usage at the cost of a slower trading session. It should be a numeric value. Default is 800 ms. Lowest value is 100 ms. _(optional)_
    - `LogLevel`: Detail level of bot's log. In order from most verbose to least verbose, valid options are:
	 - `Debug`: Information that is helpful in performing diagnostics
	 - `Info`: Generally useful information such as start/stop, polling events, etc. **Default**
	 - `Success`: Events that have completed in an expected manner
	 - `Warn`: Potential application problems, but which have been automatically handled
	 - `Error`: Event that prevents the bot from continuing to function without corrective action. 
	 - `Interface`: Events that require user interaction, such as entering a Steam Guard code to complete a login
	 - `Nothing`: A log level that surpresses all previous levels. **Not recommended**

3. Rename `settings-template.json` to `settings.json`
 
### Step 2 ###
1. Next you need to actually edit the bot to make it do what you want.
2. You can edit the file `SimpleUserHandler.cs` as it contains events for everything you need. Alternatively, you can subclass `UserHandler` and create your own class to control bot behavior. If you do this, remember to modify the `BotControlClass` setting in your configuration. Add your code to each of the events. Events are explained in code commments.
3. Look at Usage below to see some useful functions.

## Usage ##

These are a few things you can use when writing your user handler:

### Basic Steam Community ###
#### `IsAdmin` ####
Returns true if the user handler instance is for an administrator.

#### `Bot` ####
The `Bot` instance for the bot the user handler is running for.

#### `Bot.log` ####
The `Log` class for the Bot.

#### `Bot.SteamFriends.SendChatMessage(SteamID target, EChatEntryType type, string message)` ####
Send a chat message to the specified user (by steam id).

#### `Bot.SteamFriends.AddFriend(SteamID steamId)` ####
Add a friend by steam id.

### `OnTrade*` Callbacks ###
#### `Trade` ####
The master class referring back to the current trade.

#### `Trade.AddItem(ulong itemid, int slot)` ####
Add an item by its `id` property into the specified slot in the trade.

#### `Trade.AddItemByDefindex(int defindex, int slot)` ####
Same as AddItem, but you specify the defindex of the item instead of the id.

#### `Trade.RemoveItem(ulong itemid, int slot)` ####
Removes the specified item from the trade.

#### `Trade.SetReady(bool ready)` ####
Sets the trade ready or not ready according to the boolean.

#### `Trade.AcceptTrade()` ####
Accepts the trade.

#### `Trade.SendMessage(string msg)` ####
Sends a message to the other user over trade chat.

## More help? ##
If it's a bug, open an Issue; if you have a fix, open a Pull Request.  A list of contributors (add yourself if you want to):
- [Jessecar96](http://steamcommunity.com/id/jessecar) (project lead)
- [geel9](http://steamcommunity.com/id/geel9)
- [Dr. Cat, MD or redjazz96](http://steamcommunity.com/id/redjazz96)

SteamBot is licensed under the MIT License.  Check out LICENSE for more details.

## Wanna Contribute? ##
Check out CONTRIBUTING.md.
