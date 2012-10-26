using System;
using System.IO;

namespace SteamBot
{
	public class Log
	{

		enum LogLevel {
			Info,
			Debug,
			Success,
			Warn,
			Error,
			Interface, // if the user needs to input something
			Nothing    // not recommended
		}

		protected StreamWriter _FileStream;
		protected Bot _Bot;

		public bool OutputToConsole;

		public Log (string logFile, Bot bot=null, LogLevel output=LogLevel.Success)
		{
			_FileStream = File.AppendText (logFile);
			_FileStream.AutoFlush = true;
			_Bot = bot;
			OutputToConsole = output;
		}

		~Log ()
		{
			_FileStream.Close ();
		}

		public void Info (string data)
		{
			_OutputLine (LogLevel.Info, data);
		}

		public void Debug (string data)
		{
			_OutputLine (LogLevel.Debug, data);
		}

		public void Success (string data)
		{
			_OutputLine (LogLevel.Succes, data);
		}

		public void Warn (string data)
		{
			_OutputLine (LogLevel.Warn, data);
		}

		public void Error (string data)
		{
			_OutputLine (LogLevel.Error, data);
		}

		public void Interface (string data)
		{
			_OutputLine (LogLevel.Interface, data);
		}

		protected void _OutputLine (LogLevel level, string line)
		{
			string formattedString = String.Format (
				"[{0} {1}] {2}: {3}",
				(_Bot == null ? "(System)" : _Bot.DisplayName),
				DateTime.Now.ToString ("dd/MM/yyyy hh:mm:ss"),
				_LogLevel (level).ToUpper (), line
				);
			_FileStream.WriteLine (formattedString);
			if (level >= OutputToConsole)
				Console.WriteLine (formattedString);
		}

		protected string _LogLevel (LogLevel level)
		{
			switch (level)
			{
			case LogLevel.Info: 
				return "info"; 
			case LogLevel.Debug:
				return "debug";
			case LogLevel.Success:
				return "success";
			case LogLevel.Warn:
				return "warn";
			case LogLevel.Error:
				return "error";
			default:
				return "undef";
			}
		}
	}
}

