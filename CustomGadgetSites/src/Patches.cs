using HarmonyLib;
using UnityEngine;

#if DEBUG
using MonomiPark.SlimeRancher.DataModel;
using Common;
#endif

namespace CustomGadgetSites
{
	[HarmonyPatch(typeof(TargetingUI), "Update")]
	static class TargetingUI_Update_Patch
	{
		const float raycastDistance = 10f;

		static WeaponVacuum vacpack;

		static GameObject getPointedObject(out Vector3 position)
		{
			var tr = vacpack.vacOrigin.transform;
			Physics.Raycast(tr.position, tr.up, out var hit, raycastDistance);
			position = hit.point;

			return hit.collider?.gameObject;
		}

		static void Postfix()
		{
			if (!vacpack)
				vacpack = SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();

			if (vacpack.vacMode != WeaponVacuum.VacMode.GADGET)
				return;

			if (getPointedObject(out var position) is not GameObject target)
				return;

			var targetSite = target.GetComponentInParent<GadgetSite>();

			if (SRInput.Actions.attack.WasPressed)
			{
				if (targetSite)
					GadgetSiteManager.removeSite(targetSite);
				else
					GadgetSiteManager.createSite(position);
			}
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