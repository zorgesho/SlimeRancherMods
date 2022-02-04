using SRML;
using Common;

namespace SmartVacpack
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void Load() {}
		public virtual void PostLoad() {}

		public virtual void PreLoad()
		{
			init();
			HarmonyPatcher.GetInstance().PatchAll();
		}
	}
}