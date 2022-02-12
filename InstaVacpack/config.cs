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
	}
}