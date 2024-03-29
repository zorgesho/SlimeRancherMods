﻿using SRML;
using SRML.Console;

using Common;

namespace CustomGadgetSites
{
	class Main: Mod, IModEntryPoint
	{
		public static new readonly string id = Mod.id.ToLower();

		public virtual void Load()
		{
			init();
			HarmonyPatcher.GetInstance().PatchAll();

			SaveLoadUtils.init();

			Console.RegisterCommand(new RestoreGadgetSitesCommand());
#if DEBUG
			Console.RegisterCommand(new CreateGadgetSiteCommand());
#endif
		}

		public virtual void PreLoad() {}
		public virtual void PostLoad() {}
	}
}