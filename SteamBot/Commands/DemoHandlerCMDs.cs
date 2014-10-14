using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.Commands
{
    class TestCommand : CommandBase
    {
        public delegate void TestBotFunc(List<string> replies);

        private TestBotFunc myFunc;

        public TestCommand(TestBotFunc func)
        {
            cmdName = "test";
            cmdDescription = "A simple test command.";
            cmdArgs = null;
            adminCMD = false;
            cmdType = CmdType.CmdType_Trade;
        }

        public override bool OnCommand(CommandParams cParams)
        {
            myFunc(cParams.reply);
            return true;
        }
    }
}
