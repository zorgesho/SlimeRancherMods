﻿using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Common
{
	using Reflection;

	static partial class Debug
	{
		public static void logStack(string msg = "")
		{
			var stackFrames = new StackTrace().GetFrames();
			StringBuilder sb = new ($"Callstack {msg}:{Environment.NewLine}");

			for (int i = 1; i < stackFrames.Length; i++) // don't print first item, it is "logStack"
				sb.AppendLine($"\t{stackFrames[i].GetMethod().fullName()}");

			sb.ToString().log();
		}

		[Conditional("DEBUG")]
		public static void assert(bool condition, string message = null, [CallerFilePath] string __filename = "", [CallerLineNumber] int __line = 0)
		{
			if (condition)
				return;

			string msg = $"Assertion failed{(message != null? ": " + message: "")} ({__filename}:{__line})";

			$"{msg}".logError();
			throw new Exception(msg);
		}
	}
}