using SRML;
using Common;

namespace SmartVacpack
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void PreLoad()
		{
			init();
			CommonPatches.init();
		}

		// patching here for correct patch order
		public virtual void Load() => HarmonyPatcher.GetInstance().PatchAll();

		public virtual void PostLoad() {}
	}
}