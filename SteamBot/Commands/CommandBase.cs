using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamBot.Commands
{
    /// <summary>
    /// Enum to tell handler where this command can be used.
    /// </summary>
    public enum CmdType
    {
        CmdType_None = 0,
        CmdType_Console = (1 << 0),
        CmdType_Chat = (1 << 1),
        CmdType_Trade = (1 << 2),
    }
    /// <summary>
    /// Class to handle things commands need.
    /// </summary>
    public class CommandParams
    {
        /// <summary>
        /// Command handler that handled this command.
        /// </summary>
        public CommandHandler handler { get; private set; }

        /// <summary>
        /// The userhandler instance of bot that command was activated from.
        /// </summary>
        public UserHandler botHandler { get; private set; }

        /// <summary>
        /// Arguments passed to command.
        /// </summary>
        public string[] args { get; private set; }

        /// <summary>
        /// A list of strings to reply to user with.
        /// </summary>
        public List<string> reply { get; private set; }

        /// <summary>
        /// This is set with where cmd was activated.
        /// </summary>
        /// <remarks>Only 1 of the 3 bitstrings is passed here.</remarks>
        public CmdType cmdActivator { get; private set; }

        public CommandParams(CommandHandler Handler, UserHandler bot, string[] Args, CmdType activation)
        {
            handler = Handler;
            botHandler = bot;
            args = Args;
            cmdActivator = activation;
            reply = new List<string>();
        }
    }
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class CommandBase
    {

        /// <summary>
        /// This class is for information about an arguement.
        /// </summary>
        public class ArgumentInfo
        {
            /// <summary>
            /// Name of argument.
            /// </summary>
            public string argName { get; private set; }

            /// <summary>
            /// Description of argument.
            /// </summary>
            public string argDesc { get; private set; }

            /// <summary>
            /// Is argument optional?
            /// </summary>
            public bool isOptional { get; private set; }

            public ArgumentInfo(string name, string desc, bool optional)
            {
                argName = name;
                argDesc = desc;
                isOptional = optional;
            }

            public ArgumentInfo(string name, string desc)
            {
                argName = name;
                argDesc = desc;
                isOptional = false;
            }

            public override string ToString()
            {
                string argNameF = argName;
                if (isOptional)
                    argNameF = String.Format("{{{0}}}", argName);
                return String.Format("{0,-12}{1}", argNameF, argDesc);
            }
        }

        /// <summary>
        /// Name of command.
        /// </summary>
        protected string cmdName;

        /// <summary>
        /// Description of command.
        /// </summary>
        protected string cmdDescription;

        /// <summary>
        /// A list of arguments(name as key, description as value).
        /// </summary>
        protected List<ArgumentInfo> cmdArgs;

        /// <summary>
        /// Is command for admins only?
        /// </summary>
        protected bool adminCMD;

        /// <summary>
        /// Type of command.
        /// </summary>
        protected CmdType cmdType;

        public CommandBase() { }

        /// <summary>
        /// Called when a command is fired.
        /// </summary>
        /// <param name="cParams">a reference to the parameters for command.</param>
        /// <returns>True if command can be successfully used.</returns>
        public abstract bool OnCommand(CommandParams cParams);

        /// <summary>
        /// Name of command.
        /// </summary>
        public string CmdName
        {
            get
            {
                return cmdName;
            }
        }

        /// <summary>
        /// Description of command.
        /// </summary>
        public string CmdDescription
        {
            get
            {
                return cmdDescription;
            }
        }

        /// <summary>
        /// A list of arguments(name as key, description as value).
        /// </summary>
        public List<ArgumentInfo> CmdArgs
        {
            get
            {
                return cmdArgs;
            }
        }

        /// <summary>
        /// Is command for admins only?
        /// </summary>
        public bool IsAdminCmd
        {
            get
            {
                return adminCMD;
            }
        }

        /// <summary>
        /// Type of command.
        /// </summary>
        public CmdType CmdType
        {
            get
            {
                return cmdType;
            }
        }

        /// <summary>
        /// Is command of type?
        /// </summary>
        /// <param name="type">type to compare with</param>
        /// <returns>true if it's of type, false otherwise.</returns>
        public bool IsOfType(CmdType type)
        {
            return (cmdType & type) == type;
        }

        public override string ToString()
        {
            return String.Format("{0,-12}{1}", cmdName, cmdDescription);
        }
    }
}
