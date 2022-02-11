using SRML;
using Common;

namespace InstaVacpack
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void PreLoad()
		{
			init();
			CommonPatches.init();

			HarmonyPatcher.GetInstance().PatchAll();
		}

		public virtual void Load() {}
		public virtual void PostLoad() {}
	}
}