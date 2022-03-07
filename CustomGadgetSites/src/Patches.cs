using HarmonyLib;
using UnityEngine;

using Common;

#if DEBUG
using MonomiPark.SlimeRancher.DataModel;
#endif

namespace CustomGadgetSites
{
	[HarmonyPatch(typeof(TargetingUI), "Update")]
	static class TargetingUI_Update_Patch
	{
		const float raycastDistance = 10f;

		static WeaponVacuum vacpack;

		static bool getTargetPoint(out Vector3 position, out GadgetSite site)
		{
			var tr = vacpack.vacOrigin.transform;
			Ray ray = new (tr.position - tr.up * 2f, tr.up);

			bool point = Physics.Raycast(ray, out var hit, raycastDistance, 1);
			position = hit.point;

			Physics.Raycast(ray, out hit, raycastDistance, 1 << vp_Layer.RaycastOnly);
			site = hit.collider?.gameObject?.getParent()?.GetComponent<GadgetSite>();

			return point || site;
		}

		static void processLeftClick(Vector3 position, GadgetSite site)
		{
			if (!SRInput.Actions.attack.WasPressed || position == default)
				return;

			if (site)
				GadgetSiteManager.removeSite(site);
			else
				GadgetSiteManager.createSite(position);
		}

		static void Postfix()
		{
			if (!vacpack)
				vacpack = SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();

			if (vacpack.vacMode != WeaponVacuum.VacMode.GADGET)
				return;

			if (!getTargetPoint(out var position, out var targetSite))
				return;

			processLeftClick(position, targetSite);
		}
	}

#if DEBUG
	static class DebugPatches
	{
		static readonly bool includeVanillaSites = false;

		static bool shouldLogSite(string siteId) => includeVanillaSites || siteId.Contains(".");

		[HarmonyPatch(typeof(GameModel), "RegisterGadgetSite")]
		static class GameModel_RegisterGadgetSite_Patch
		{
			static void Postfix(string siteId, GameObject gameObject)
			{
				if (shouldLogSite(siteId))
					$"Gadget site registered: {siteId} ({gameObject.name})".logDbg();
			}
		}

		[HarmonyPatch(typeof(GameModel), "UnregisterGadgetSite")]
		static class GameModel_UnregisterGadgetSite_Patch
		{
			static void Postfix(string siteId)
			{
				if (shouldLogSite(siteId))
					$"Gadget site unregistered: {siteId}".logDbg();
			}
		}
	}
#endif // DEBUG
}