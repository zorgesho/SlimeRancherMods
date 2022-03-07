using SRML;
using Common;

#if DEBUG
using SRML.Console;
#endif

namespace CustomGadgetSites
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void PreLoad()
		{
			init();
			HarmonyPatcher.GetInstance().PatchAll();

			SaveLoadUtils.init();
#if DEBUG
			Console.RegisterCommand(new CreateGadgetSiteCommand());
#endif
		}

		public virtual void Load() {}
		public virtual void PostLoad() {}
	}
}