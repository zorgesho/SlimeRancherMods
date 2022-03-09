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
			public readonly string id; // either number (for sites created by this mod) or full id (for other sites)
			public Vector3 position;

			public bool isRemoved
			{
				set
				{
					Common.Debug.assert(value);
					position = default;
				}

				get => position == default;
			}

			public bool isExternal => id.startsWith("site");

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

		static GadgetSiteInfo getSiteInfo(GadgetSite site)
		{
			if (!_sites.TryGetValue(site.id, out var siteInfo))
			{
				siteInfo = new GadgetSiteInfo(site.id, site.transform.position);																	$"GadgetSiteManager.getSiteInfo: external site added ('{site.id}')".logDbg();
				_sites[site.id] = siteInfo;
			}

			return siteInfo;
		}

		public static bool moveSite(GadgetSite site, Vector3 position)
		{
			if (!site || site.attached)
				return false;

			var siteInfo = getSiteInfo(site);
			site.transform.position = position;
			siteInfo.position = position;

			return true;
		}

		public static bool removeSite(GadgetSite site)
		{																																			$"GadgetSiteManager.removeSite: trying to remove '{site?.id}'".logDbg();
			if (!site || site.attached)
				return false;

			var siteInfo = getSiteInfo(site);

			if (siteInfo.isExternal)
			{
				siteInfo.isRemoved = true;
				site.transform.gameObject.SetActive(false);																							"GadgetSiteManager.removeSite: external site deactivated".logDbg();
			}
			else
			{
				// for some reason sites without attached objects are not unregister
				SRSingleton<SceneContext>.Instance.GameModel.UnregisterGadgetSite(site.id);

				_sites.Remove(site.id);
				Object.Destroy(site.gameObject);																									"GadgetSiteManager.removeSite: site removed".logDbg();
			}

			return true;
		}

		public static void loadSitesInfo(IEnumerable<GadgetSiteInfo> sites)
		{																																			"GadgetSiteManager.loadSitesInfo".logDbg();
			_sites.Clear();
			_sitesInfoProcessed = false;

			foreach (var site in sites)
			{
				Common.Debug.assert(site != null);

				if (site == null)
					continue;

				if (site.isExternal)
				{
					_sites[site.id] = site;
				}
				else
				{
					_sites[claimID(site)] = site;
					maxID = int.Parse(site.id) + 1;
				}																																	$"GadgetSiteManager.loadSitesInfo: site loaded (id: '{site.id}', pos: {site.position})".logDbg();
			}																																		$"GadgetSiteManager.loadSitesInfo: {_sites.Count} loaded".logDbg();
		}

		public static void processSitesInfo()
		{																																			"GadgetSiteManager.processSitesInfo".logDbg();
			Common.Debug.assert(!_sitesInfoProcessed);

			var gameModel = SRSingleton<SceneContext>.Instance.GameModel;

			foreach (var site in sites)
			{
				_sitesInfoProcessed = true;

				if (!site.isExternal)
				{
					createSite(site);
					continue;
				}

				if (!gameModel.gadgetSites.TryGetValue(site.id, out var siteModel))
				{
					$"Gadget site with id '{site.id}' not found".logError();
					continue;
				}

				if (site.isRemoved)
					siteModel.transform.gameObject.SetActive(false);
				else
					siteModel.transform.position = site.position;
			}
		}
	}
}