using SRML.Console;

#if DEBUG
using UnityEngine;
#endif

namespace CustomGadgetSites
{
#if DEBUG
	class CreateGadgetSiteCommand: ConsoleCommand
	{
		public override string ID => "creategadgetsite";
		public override string Usage => ID;
		public override string Description => "Creates a gadget site";

		public override bool Execute(string[] _)
		{
			var tr = Camera.main.transform;

			if (!Physics.Raycast(tr.position, tr.forward, out var raycastHit))
				return false;

			return GadgetSiteManager.createSite(raycastHit.point);
		}
	}
#endif // DEBUG

	class RestoreGadgetSitesCommand: ConsoleCommand
	{
		public override string ID => "restoregadgetsites";
		public override string Usage => ID;
		public override string Description => "Restores moved or removed vanilla gadget sites. Reloading required.";

		public override bool Execute(string[] _)
		{
			int restoredSites = GadgetSiteManager.restoreExternalSites();

			if (restoredSites > 0)
				Console.Log($"{restoredSites} site{(restoredSites > 1? "s were": " was")} restored. Reload your save to apply.");
			else
				Console.Log("No sites found to restore");

			return true;
		}
	}
}