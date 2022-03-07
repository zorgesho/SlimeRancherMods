#if DEBUG
using UnityEngine;
using SRML.Console;

namespace CustomGadgetSites
{
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
}
#endif // DEBUG