## Info ##

**BEFORE YOU GET STARTED:** This bot requires you use git to download the bot.  **Downloading the zip will not work!**  It doesn't work because the bot uses git submodules, which are not included with the zip download.

**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  As of right now, about 8 contributors have all added to the bot.  The bot is publicly available under the MIT License.

## Configuration Instructions ##

### Step 0 ###
If you've just recently cloned this repository, there are a few things you need to do in order to build the source code.

1. Run `git submodule init` to initalize the submodule configuration file.
2. Run `git submodule update` to pull the latest version of the submodules that are included (namely, SteamKit2).
 - Since SteamKit2 is licensed under the LGPL, and SteamBot should be released under the MIT license, SteamKit2's code cannot be included in SteamBot.  This includes executables.  We'll probably make downloads available on GitHub.
3. Open `SteamBot.sln` in your C# development environment, either MonoDevelop or Visual Studio should work.
4. Build the program.

### Step 1 ###
1. First, you need to configure your bots. This is done by creating a `settings.json` file located in the same place as the SteamBot executable.
2. There is a template settings file provided for you. Edit the file `settings-template.json` in `project_root\Bin\Debug`.  Some configuration options:
   - `Admins`: An array of Steam Profile IDs of the users that are an Admin of your bot(s). Each Profile ID should be a string in quotes and seperated by a comma. These admins are global to all bots listed in the `Bots` array.
   - `ApiKey`: The API key you have been assigned by Valve. If you do not have one, it can be requested from Value at their [Web API Key](http://steamcommunity.com/dev/apikey) page. **This is required and the bot(s) will not work without an API Key**. The API Key should be a string in quotes.
   - `mainLog`: The log containing runtime information for all bots.
   - `UseSeparateProcesses`: Determines whether or not bot manager opens each bot in it's own process. Default is `false`.
   - `Bots`: An array of dictionaries containing information about each individual bot you will be running. You can run multiple bots at the same time by having multiple elements in the `Bots` array. Each entry in the `Bots` array consists of the following values:
    - `Username`: The Steam user name for this bot. It should be a string in quotes. _(required)_
    - `Password`: The password for the Steam user associated with this bot. It should be a string in quotes. _(required)_
    - `DisplayName`: The name the bot will present on Steam. It should be a string in quotes. _(required)_
    - `ChatResponse`: This is the response the bot will provide when a user chats with it via Steam Friends. It should be a string in quotes. _(required)_
    - `logFile`: The log file for this specific bot. It should be a string in quotes. _(required)_
    - `BotControlClass`: The fully qualified class that controls how this specific bot will behave. Generally, this is a seperate file (ie. `SimpleUserHandler.cs`) and has the same name as your class (without the trailing `.cs` extension). It must be the fully qualified class (ie. `SteamBot.SimpleUserHandler`). It should be a string in quotes. _(required)_
    - `Admins`: Additional admins, specific to this bot. _(optional)_
    - `MaximumTradeTime`: Maximium length of time for a trade session (in seconds). It should be a numeric value. Defaults to 180 seconds. _(optional)_
    - `MaximumActionGap`: Length of time the bot will allow the user to remain inactive. It should be a numeric value. Defaults to 30 seconds. _(optional)_
    - `DisplayNamePrefix`: A prefix to display in front of the DisplayName. It should be a string encloded by quotes. Defaults to an empty string. _(optional)_
    - `TradePollingInterval`: Length of time, in milliseconds, between polling events. Higher values reduce CPU usage at the cost of a slower trading session. It should be a numeric value. Default is 800 ms. Lowest value is 100 ms. _(optional)_
    - `LogLevel`: Detail level of bot's log. In order from most verbose to least verbose, valid options are:
	 - `Debug`: Information that is helpful in performing diagnostics.
	 - `Info`: Generally useful information such as start/stop, polling events, etc. _(default)_
	 - `Success`: Events that have completed in an expected manner.
	 - `Warn`: Potential application problems, but which have been automatically handled.
	 - `Error`: Event that prevents the bot from continuing to function without corrective action.
	 - `Interface`: Events that require user interaction, such as entering a Steam Guard code to complete a login.
	 - `Nothing`: A log level that surpresses all previous levels. _(not recommended)_

3. Rename `settings-template.json` to `settings.json`

### Step 2 ###
1. Next you need to actually edit the bot to make it do what you want.
2. You can edit the files `SimpleUserHandler.cs` or `AdminUserHandler.cs` as they contains examples of most everything you need. Alternatively, you can subclass `UserHandler` and create your own class to control bot behavior. If you do this, remember to modify the `BotControlClass` setting in your configuration. Add your code to each of the events. Events are explained in code comments.
3. Look at the section below to see some useful information if you intend to create your own `UserHandler`.

### Step 3 ###
1. Run the SteamBot executable. 
 - Open your operating systems console or command prompt.
 - Run the executable (SteamBot.exe under Windows). After a successful build it should be under `<project_root>\Bin\Debug` by default. 

## UserHandlers ##

In order to fully customize your bot you are going to want to create a class that inherits from `SteamBot.UserHandler`. This class is an abstract base class that provides several methods that *must be overridden* in order to work. These methods are mostly reactionary in nature, i.e. what to do when the bot has been proposed a trade or sent a message. These explained well in `UserHandler.cs` code comments. Here is a basic run-down of what's available to your subclass if you decide to do this.

### Steam Community ###

#### `UserHandler.IsAdmin` ####
Returns `true` if the other user interacting with the bot is one of the configured Admins. See settings.json format above.

#### `UserHandler.Log` ####
The `Log` class for the Bot that you can use this to output important information to the console you see on the screen.

#### `UserHandler.Bot` ####
The `Bot` instance for the bot the user handler is running for. You can use this to access some advanced features of the Bot like the Steam Friends system below.

#### `UserHandler.Bot.SteamFriends.SendChatMessage(SteamID target, EChatEntryType type, string message)` ####
Send a chat message to the specified user (by steam id).

#### `UserHandler.Bot.SteamFriends.AddFriend(SteamID steamId)` ####
Add a friend by steam id.

#### `UserHandler.Bot.SteamFriends.RemoveFriend(SteamID steamId)` ####
Remove a friend by steam id.

### Trade Support ###

Most of the trade interaction will occur through the abstract methods that you will have to implement as a subclass. These are mostly Trade events that happened outside of the Bot. For example `OnTradeAddItem` is called when the other user adds an item to the trade window. In this function your class could add equal, lesser, or greater value items (or send the user a nasty message about low-balling). To do this you will have to interact with the trade via the `UserHandler.Trade` object described below.


#### `UserHandler.Trade.AddItem(ulong itemid)` ####
Add an item by its `id` property into the next available slot in the trade window.

#### `UserHandler.Trade.AddItemByDefindex(int defindex)` ####
Same as AddItem, but you specify the defindex of the item instead of the id.

#### `UserHandler.Trade.RemoveItem(ulong itemid)` ####
Removes the specified item from the trade window.

#### `UserHandler.Trade.SetReady(bool ready)` ####
Sets the trade ready-status depending on the `ready` parameter. `true` to set the bot's status to ready.

#### `UserHandler.Trade.AcceptTrade()` ####
Calling this method accepts the trade. It's the second step after both parties ready-up.

#### `UserHandler.Trade.SendMessage(string msg)` ####
Sends a message to the other user over trade chat.

## Bot Manager Commands ##
The Bot Manager manages how the bots behave, whether in separate processes or in separate threads. It also allows the user/administrator to interact with the bots via the console window.

`start <index in configuration>`: Start the bot that is at the index specified in `settings.json`. It is a 0 based index.
`stop <index in configuration>`: Stop the both that is at the index specified in `settings.json`. It is a 0 based index. 
`show bots`: Dump the bot configuration to the console
`help`: Shows available commands

The `UseSeparateProcesses` option from `settings.json` controls whether the bots will remain in one console and use a threading mechanism to handle the Steam interaction. This prevents exceptions in one running bot from interfering with other bots.




## More help? ##
If it's a bug, open an Issue; if you have a fix, read [CONTRIBUTING.md](https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md) and open a Pull Request.  A list of contributors (add yourself if you want to):
- [Jessecar96](http://steamcommunity.com/id/jessecar) (project lead)
- [geel9](http://steamcommunity.com/id/geel9)
- [Dr. Cat, MD or redjazz96](http://steamcommunity.com/id/redjazz96)
- [cwhelchel](http://steamcommunity.com/id/cmw69krinkle)

SteamBot is licensed under the MIT License.  Check out LICENSE for more details.

## Wanna Contribute? ##
Please read [CONTRIBUTING.md](https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md).
