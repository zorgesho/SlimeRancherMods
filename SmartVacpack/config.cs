#if !DEBUG
using SRML.Config.Attributes;
#endif

namespace SmartVacpack
{
#if !DEBUG
	[ConfigFile("config")]
#endif
	static class Config
	{
		public static readonly bool preferEmptySlots = false;
		public static readonly bool returnDroppedToSilo = true;
	}
}