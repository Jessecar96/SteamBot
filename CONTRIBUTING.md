# Contributing to SteamBot #
When you're contributing to SteamBot, there are a few rules you should follow.  First and foremost, SteamBot should be able to compile and run on Linux.  SteamBot development works in both Visual Studio and MonoDevelop, but _please_ keep your temporary files (such as `.pidb`, `.*~`, or even `.tmp`) out of the project.

## How To Contribute ##
1. Fork The Repository ([Jessecar96/SteamBot](https://github.com/Jessecar96/SteamBot))
2. Branch It
	- this is because when you do the pull request for it, it includes commits you make after you make the pull request and before the pull request is accepted
3. Make Your Changes
4. Commit Your Changes
5. Do 3 and 4 as Needed
6. Push Your Changes Back to GitHub
7. Start a Pull Request on the Repository
	- make sure you explain what the pull request does

## Indentation ##
With SteamBot, you should use four (4) spaces as an indent; tabs should not be used as indentation ever.  This comes from
Microsoft's [C# Coding Conventions](http://msdn.microsoft.com/en-us/library/vstudio/ff926074.aspx) (thank you, Philipp).  It gets annoying when you have both in there, and it clogs up commit logs trying to fix it.

## Brackets ##
Brackets should be on the next line of a function definition or an if directive.  Brackets should always be on their own line.

## Issues ##
Make sure you
- Describe the problem
- Show how to reproduce it, if applicable
- Explain what you think is causing it, if applicable
- Give a plausible solution

## Commits ##
Commits should be in the present tense, and with Title Capitalization.  If needed, a body should be on the next line in normal capitalization.
