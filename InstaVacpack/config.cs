#if !DEBUG
using SRML.Config.Attributes;
#endif

namespace InstaVacpack
{
#if !DEBUG
	[ConfigFile("config")]
#endif
	static class Config
	{
#if DEBUG
		public static class Dbg
		{
			public static readonly bool moneyCheat = true;
			public static readonly bool showColliders = true;
		}
#endif
	}
}