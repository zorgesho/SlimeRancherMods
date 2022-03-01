using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using SRML.SR.Utils;
using SRML.SR.SaveSystem;

using Common;

namespace CustomGadgetSites
{
	static class GadgetSiteManager
	{
		public class CustomGadgetSite
		{
			public readonly string id;
			public Vector3 position;

			public CustomGadgetSite(string id, Vector3 position)
			{
				this.id = id;
				this.position = position;
			}
		}

		static int maxID;

		static readonly Dictionary<string, CustomGadgetSite> _sites = new();
		public static IEnumerable<CustomGadgetSite> sites => _sites.Values;

		static string claimID(CustomGadgetSite site) => ModdedStringRegistry.ClaimID("site", site.id);

		public static void createSite(Vector3 position) => createSite(new ($"{maxID++}", position), true);

		static void createSite(CustomGadgetSite site, bool register = false)
		{																																			$"GadgetSiteManager.createSite: {site.id} {site.position}".logDbg();
			string id = claimID(site);
			var cellRoot = RegionUtils.GetRegionsFromPosition(site.position)[0].cellDir.transform;
			var parent = cellRoot.Find("Sector/Build Sites") ?? cellRoot;																			$"GadgetSiteManager.createSite: parent is '{parent.name}'".logDbg(); // TODO fullname
			IdHandlerUtils.CreateIdInstance<GadgetSite>(id, site.position, parent);

			if (register)
				_sites[id] = site;
		}

		public static void loadSites(IEnumerable<CustomGadgetSite> sites)
		{
			_sites.Clear();

			foreach (var site in sites)
			{
				Common.Debug.assert(site != null);

				if (site == null)
					continue;

				_sites[claimID(site)] = site;
				maxID = int.Parse(site.id) + 1;
			}																																		$"GadgetSiteManager.loadSites: {_sites.Count} loaded".logDbg();
		}

		public static void createLoadedSites() // TODO check for multiple creation ?
		{																																			"GadgetSiteManager.createSites".logDbg();
			sites.ToList().ForEach(s => createSite(s)); // TODO
		}
	}
}