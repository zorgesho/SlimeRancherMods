using SRML;
using Common;

namespace CustomGadgetSites
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void PreLoad()
		{
			init();
			HarmonyPatcher.GetInstance().PatchAll();
		}

		public virtual void Load() {}
		public virtual void PostLoad() {}
	}
}