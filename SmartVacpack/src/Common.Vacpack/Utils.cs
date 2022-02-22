using HarmonyLib;
using UnityEngine;

namespace Common.Vacpack
{
	static class Utils
	{
		public static int frameVacModeChanged => VacModeWatcher.lastFrameChanged;

		public static GameObject tryGetPointedObject(WeaponVacuum vacpack, bool interactableLayerOnly = true, float distance = 10f)
		{
			var tr = vacpack.vacOrigin.transform;
			Ray ray = new (tr.position, tr.up);

			bool result = Physics.Raycast(ray, out var hit, distance, 1 << vp_Layer.Interactable, QueryTriggerInteraction.Collide);

			if (!result && !interactableLayerOnly)
				Physics.Raycast(ray, out hit, distance);

			return hit.collider?.gameObject;
		}

		public static T tryGetPointedObject<T>(WeaponVacuum vacpack, bool interactableLayerOnly = true, float distance = 10f) where T: Component
		{
			return tryGetPointedObject(vacpack, interactableLayerOnly, distance)?.GetComponent<T>();
		}


		[HarmonyPatch(typeof(WeaponVacuum), "UpdateVacModeForInputs")]
		static class VacModeWatcher
		{
			public static int lastFrameChanged { get; private set; }

			static WeaponVacuum.VacMode prevVacMode;

			static void Postfix(WeaponVacuum __instance)
			{
				if (__instance.vacMode == prevVacMode)
					return;

				prevVacMode = __instance.vacMode;
				lastFrameChanged = Time.frameCount;																	$"VacModeWatcher: vac mode {prevVacMode}, frame: {lastFrameChanged}".logDbg();
			}
		}
	}
}