using SRML;
using SRML.Console;

using Common;

namespace CustomGadgetSites
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void PreLoad()
		{
			init();
			HarmonyPatcher.GetInstance().PatchAll();

			SaveLoadUtils.init();
			Console.RegisterCommand(new CreateGadgetSiteCommand());
		}

		public virtual void Load() {}
		public virtual void PostLoad() {}
	}
}