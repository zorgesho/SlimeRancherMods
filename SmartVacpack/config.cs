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
		public static readonly bool sameMultipleSlots = true;
		public static readonly bool returnDroppedToSilo = true;
		public static readonly bool showAdditionalInfo = true;
		public static readonly bool allowToSwitchSiloSlotsManually = true;
		public static readonly bool preferEmptySlots = false;

		public static readonly bool highlightSelectedSlot = true;
		public static readonly string highlightColor = "#FFFF00";
	}
}