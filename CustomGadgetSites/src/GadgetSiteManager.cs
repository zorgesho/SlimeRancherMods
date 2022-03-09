using System.Collections.Generic;

using UnityEngine;

using SRML.SR.Utils;
using SRML.SR.SaveSystem;

using Common;

namespace CustomGadgetSites
{
	static class GadgetSiteManager
	{
		public class GadgetSiteInfo
		{
			public readonly string id;
			public Vector3 position;

			public GadgetSiteInfo(string id, Vector3 position)
			{
				this.id = id;
				this.position = position;
			}
		}

		static int maxID;
		static bool _sitesInfoProcessed; // just in case

		static readonly Dictionary<string, GadgetSiteInfo> _sites = new();
		public static IEnumerable<GadgetSiteInfo> sites => _sites.Values;

		static string claimID(GadgetSiteInfo site) => ModdedStringRegistry.ClaimID("site", site.id);

		public static bool createSite(Vector3 position) => createSite(new ($"{maxID++}", position), true);

		static Transform findSitesParent(Vector3 position)
		{
			var regions = RegionUtils.GetRegionsFromPosition(position);

			if (regions.Count == 0)
				return null;

			var sectorRoot = regions[0].cellDir.transform.Find("Sector");
			return sectorRoot?.Find("Build Sites") ?? sectorRoot;
		}

		static bool createSite(GadgetSiteInfo site, bool register = false)
		{																																			$"GadgetSiteManager.createSite: {site.id} {site.position}".logDbg();
			if (findSitesParent(site.position) is not Transform sitesParent)
				return false;

			string id = claimID(site);																												$"GadgetSiteManager.createSite: parent is '{sitesParent.gameObject.getFullName()}', id is: {id}".logDbg();

			var newSite = IdHandlerUtils.CreateIdInstance<GadgetSite>(id, site.position, sitesParent).GetComponent<GadgetSite>();
			newSite.director = sitesParent.GetComponentInParent<IdDirector>();
			newSite.director.persistenceDict[newSite] = id;

			if (register)
				_sites[id] = site;

			return true;
		}

		public static bool moveSite(GadgetSite site, Vector3 position)
		{
			if (!site || site.attached)
				return false;

			if (!_sites.ContainsKey(site.id))
				return false; // TODO

			site.transform.position = position;
			_sites[site.id].position = position;

			return true;
		}

		public static bool removeSite(GadgetSite site)
		{																																			$"GadgetSiteManager.removeSite: trying to remove '{site?.id}'".logDbg();
			if (!site || site.attached)
				return false;

			if (!_sites.ContainsKey(site.id))
				return false; // TODO

			// for some reason sites without attached objects are not unregister
			SRSingleton<SceneContext>.Instance.GameModel.UnregisterGadgetSite(site.id);

			_sites.Remove(site.id);
			Object.Destroy(site.gameObject);																										"GadgetSiteManager.removeSite: site removed".logDbg();
			return true;
		}

		public static void loadSitesInfo(IEnumerable<GadgetSiteInfo> sites)
		{
			_sites.Clear();
			_sitesInfoProcessed = false;

			foreach (var site in sites)
			{
				Common.Debug.assert(site != null);

				if (site == null)
					continue;

				_sites[claimID(site)] = site;
				maxID = int.Parse(site.id) + 1;
			}																																		$"GadgetSiteManager.loadSitesInfo: {_sites.Count} loaded".logDbg();
		}

		public static void processSitesInfo()
		{																																			"GadgetSiteManager.processSitesInfo".logDbg();
			Common.Debug.assert(!_sitesInfoProcessed);
			sites.forEach(s => createSite(s));

			_sitesInfoProcessed = true;
		}
	}
}