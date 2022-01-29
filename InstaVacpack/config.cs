using UnityEngine;

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
		public static KeyCode instantModeKey = KeyCode.LeftControl;

#if DEBUG
		public static class Dbg
		{
			public static readonly bool playerCheats = true;
			public static readonly bool showColliders = false;
		}
#endif
	}
}