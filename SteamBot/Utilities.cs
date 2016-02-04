using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamBot
{
    public class Utilities
    {
        public bool DoesMessageStartWith(string Message, List<string> Comparison)
        {
            string[] words = Message.Split(' ');
            if (words.Length > 0)
            {
                string firstWord = words[0];
                return Comparison.Any(CommandWord => firstWord.Equals(CommandWord, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }
    }
}
