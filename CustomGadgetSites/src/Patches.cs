using HarmonyLib;
using UnityEngine;

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
				if (!targetSite)
					GadgetSiteManager.createSite(position);
			}
		}
	}
}