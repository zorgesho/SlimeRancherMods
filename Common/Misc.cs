using System;
using System.IO;

// fix for C# 9.0 (pre-5.0 .NET) for init-only properties
namespace System.Runtime.CompilerServices { static class IsExternalInit {} }

namespace Common
{
	static partial class StringExtensions
	{
		public static bool isNullOrEmpty(this string s) => string.IsNullOrEmpty(s);
		public static bool startsWith(this string s, string str) => s.StartsWith(str, StringComparison.Ordinal);

		public static void saveToFile(this string s, string localPath)
		{
			try { File.WriteAllText(_formatFileName(localPath), s); }
			catch (Exception e) { Log.msg(e); }
		}

		public static void appendToFile(this string s, string localPath)
		{
			try { File.AppendAllText(_formatFileName(localPath), s + Environment.NewLine); }
			catch (Exception e) { Log.msg(e); }
		}

		static string _formatFileName(string filename) => Paths.formatFileName(filename, "txt");
	}
}