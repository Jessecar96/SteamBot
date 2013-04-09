**SteamBot** is a bot written in C# for the purpose of interacting with Steam Chat and Steam Trade.  The bot is publicly available under the MIT License. If you would like to contribute to the development of **SteamBot**, we invite you to fork the repository and [contribute](https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md) your changes back.


**BEFORE YOU GET STARTED:** This bot requires you use git to download the bot.  **Downloading the zip provided by GitHub will not work!**  It doesn't work because the bot uses git submodules, which are not included with the zip download.

## Configuration Instructions ##

### Step 1 ###
When you initially clone this repository, there are a few things that need to be done one time in order to build the source code. After the clone is complete, follow these steps.

1. Run `git submodule init` to initalize the submodule configuration file.
2. Run `git submodule update` to pull the latest version of the submodules that are included (namely, SteamKit2).
 - Since SteamKit2 is licensed under the LGPL, and SteamBot should be released under the MIT license, SteamKit2's code cannot be included in SteamBot.  This includes executables.  We'll probably make downloads available on GitHub.
3. Open `SteamBot.sln` in your C# development environment, either MonoDevelop or Visual Studio should work.
4. Build the program. 
  - If you are using MonoDevelop and get an error similar to `The type or namespace name 'ProtoBuf' could not be found in the global namespace`, the solution is to build the solution twice. This error occurs because MonoDevelop doesn't contain a way to specify build order, and certain solutions within the project must be built first. Another option is to use Visual Studio 2010. Note that this project is currently built for Visual Studio 2010. If you are using a new version, you may need to respecify build order.
  
### Step 2 ###
This step contains information related to **SteamBot** configuration. These configuration options are all placed in the `settings.json` file. Parameters that are `required` will cause the bot to fail if they do not exist. `Optional` options will be defaulted if they do not exist.

1. There is a template settings file provided for you. Edit the file `settings-template.json` in `project_root\Bin\Debug` and `project_root\Bin\Release`. This file should be **renamed** to `settings.json`. The file you edit will depend on whether you are building the project in `Debug` or `Release` mode. The file can (and probably should be) identical in both locations.
2.   The `settings.json` has options described below. Edit them to match your criteria:
   - `Admins`: An array of Steam Profile IDs (_not_ Steam IDs) of the users that are an Admin of your **SteamBot**. Each Profile ID should be a string in quotes and seperated by a comma. These admins are global to all bots listed in the `Bots` array.
   - `ApiKey`: The API key you have been assigned by Valve. If you do not have one, it can be requested from Value at their [Web API Key](http://steamcommunity.com/dev/apikey) page. **This is required and the bot(s) will not work without an API Key**. The API Key should be a string in quotes.
   - `mainLog`: The log containing runtime information for all bots.
   - `UseSeparateProcesses`: Determines whether or not bot manager opens each bot in it's own process. Default is `false`. More information about the bot manager is below.
   - `Bots`: An array of dictionaries containing information about each individual bot you will be running. You can run multiple bots at the same time by having multiple elements in the `Bots` array. Each entry in the `Bots` array consists of the following values:
    - `Username`: The Steam user name for this bot. It should be a string in quotes. _(required)_
    - `Password`: The password for the Steam user associated with this bot. It should be a string in quotes. _(required)_
    - `DisplayName`: The name the bot will present on Steam. It should be a string in quotes. _(required)_
    - `ChatResponse`: This is the response the bot will provide when a user chats with it via Steam Friends. It should be a string in quotes. _(required)_
    - `logFile`: The log file for this specific bot. Each bot in the `Bots` array needs a _unique_ file. Sharing files is not permitted and will cause an error. It should be a string in quotes. _(required)_
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

	 
Ensure that you renamed `settings-template.json` to `settings.json`. 

When you first launch a **SteamBot**, you will hit a SteamGuard error. Check your associated email account and type the validation code into the prompt provided. After this, you should not run into SteamGuard problems for this project and on this development machine again. 

### Step 3 ###
1. Next you need to actually edit the bot to make it do what you want. You can edit the files `SimpleUserHandler.cs` or `AdminUserHandler.cs` as they contains examples of most everything you need. However, it is recommended that you subclass `UserHandler` and create your own class to control bot behavior. If you do this, remember to modify the `BotControlClass` setting in your configuration. Add your code to each of the events. Events are explained in code comments. Subclassing and creating your own control class is recommended because updates to **SteamBot** from other developers may over write your changes when you merge updates into your code.
3. Information about `UserHandlers` is listed below.

### Step 4 ###
1. Run the SteamBot executable. 
 - Open your operating systems console or command prompt.
 - Run the executable (SteamBot.exe under Windows). After a successful build it should be under `<project_root>\Bin\Debug` by default. After you have successfully tested your **SteamBot**, it is recommended that you create a `Release` build and run that instead. Remember to migrate your `settings.json` file to the `release` folder.
 
 
 
## UserHandlers ##

In order to fully customize your bot you are going to want to create a class that inherits from `SteamBot.UserHandler`. This class is an abstract base class that provides several methods that *must be overridden* in order to work. These methods are mostly reactionary in nature, i.e. what to do when the bot has been proposed a trade or sent a message. Here is a basic run-down of what's available to your subclass if you decide to do this. These explained well in `UserHandler.cs` code comments.

### Steam Community ###

#### `UserHandler.IsAdmin` ####
Returns `true` if the other user interacting with the bot is one of the configured Admins. See `settings.json` format above.

#### `UserHandler.Log` ####
The `Log` class for the Bot that you can use this to output important information to the console you see on the screen.

#### `UserHandler.Bot` ####
The `Bot` instance for the bot the user handler is running for. You can use this to access some advanced features of the Bot like the Steam Friends system below.

 - `UserHandler.Bot.SteamFriends.SendChatMessage(SteamID target, EChatEntryType type, string message)`: Send a chat message to  the specified user (by profile id).
 - `UserHandler.Bot.SteamFriends.AddFriend(SteamID steamId)`: Add a friend (by profile id).
 - `UserHandler.Bot.SteamFriends.RemoveFriend(SteamID steamId)`: Remove a friend (by profile id).
 
##### Methods to Override #####
These abstract methods need to be over riden in your customized UserHandler. They are called when certain events occur.

 - `OnFriendAdd`: Called when the user adds the bot as a friend.
 - `OnFriendRemove`: Called when the user removes the bot as a friend.
 - `OnMessage`: Called when a message is received via Steam Chat.
 - `OnTradeRequest`: Called when a user sends the bot a trade request.
 - `OnTradeError`: Called if an error occurs during trading.
 - `OnTradeTimeout`: Called if `MaximumTradeTime` or `MaximumActionGap` is exceeded.
 - `OnTradeInit`: Called when a trade is initiated (occurs after `OnTradeRequest`).
 - `OnTradeAddItem`: Called when an item is added to the trade.
 - `OnTradeRemoveItem`: Called when an item is removed from the trade.
 - `OnTradeMessage`: Called when a message is received in Trade Chat (not to be confused with `OnMessage`, which is for Steam Chat).
 - `OnTradeReady`: Called when user changes their Ready Status.
 - `OnTradeAccept`: Called when the user accepts the trade.
 
 
  
### Trade Support ###

Most of the trade interaction will occur through the abstract methods that you will have to implement as a subclass. These are mostly Trade events that happened outside of the Bot. For example `OnTradeAddItem` is called when the other user adds an item to the trade window. In this function your class could add it's own items to the trade. To do this you will have to interact with the trade via the `UserHandler.Trade` object described below.

#### UserHandler.Trade ####
The `Trade` instance that is currently active. This is used to interact with the Steam Trading system. 
 
  - `UserHandler.Trade.AddItem(ulong itemid)`: Adds an item by its `id` property from the game schema to the next available slot in the trade window.
  - `UserHandler.Trade.AddItemByDefindex(int defindex)`: Adds a single item by its `defindex` property from the game schema to the next available slot in the trade window.
  - `UserHandler.Trade.AddAllItemsByDefindex(int defindex)`: Adds all items with the provided `defindex` to successive slots in the trade.
  - `UserHandler.Trade.RemoveItem(ulong itemid)`: Removes the specified item, by `id`, from the trade window. 
  - `UserHandler.Trade.RemoveItemByDefindex(int defindex)`: Removes a single item by its `defindex` property from the trade.
  - `UserHandler.Trade.RemoveAllItemByDefindex(int defindex)`: Removes all items with the provided `defindex` from the trade.
  - `UserHandler.Trade.SetReady(bool ready)`: Toggles the trade's ready status. If `true`, the **SteamBot** sets its status to Ready. If `false`, the status is not Ready.
  - `UserHandler.Trade.AcceptTrade()`: This accepts the trade. It can only succeed after both parties have set their status to Ready.
  - `UserHandler.Trade.SendMessage(string msg)`: Sends a message to the other user in the trade chat. This is not to be confused with `UserHandler.Bot.SteamFriends.SendChatMessage`, which sends messages via Steam Chat.
  - `UserHandler.Trade.CancelTrade`: Cancels the current trade.
 

 
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


SteamBot is licensed under the MIT License.  Check out [LICENSE](https://github.com/Jessecar96/SteamBot/blob/master/LICENSE) for more details.

## Want to Contribute? ##
Please read [CONTRIBUTING.md](https://github.com/Jessecar96/SteamBot/blob/master/CONTRIBUTING.md).
