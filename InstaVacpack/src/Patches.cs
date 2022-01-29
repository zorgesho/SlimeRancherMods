using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common;

#if DEBUG
using Common.UnityDebug;
#endif

namespace InstaVacpack
{
	[HarmonyPatch(typeof(SiloCatcher), "OnTriggerStay")]
	static class SiloCatcher_OnTriggerStay_Patch
	{
		static bool Prefix(SiloCatcher __instance, Collider collider)
		{
			if (!__instance.hasStarted || !__instance.isActiveAndEnabled || !__instance.type.HasOutput() || Time.time < __instance.nextEject)
				return false;

			var siloActivator = collider.GetComponentInParent<SiloActivator>();

			if (siloActivator == null || !siloActivator.enabled)
				return false;

			Vector3 dir = (collider.gameObject.transform.position - __instance.transform.position).normalized;

			if (Mathf.Abs(Vector3.Angle(__instance.transform.forward, dir)) > 45f)
				return false;

			if (!__instance.Remove(out Identifiable.Id id))
				return false;

			var prefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(id);
			var vacuumable = SRBehaviour.InstantiateActor(prefab, __instance.region.setId, __instance.transform.position + dir * 1.2f, __instance.transform.rotation, false).GetComponent<Vacuumable>();
			__instance.vac.ForceJoint(vacuumable);
			__instance.nextEject = Time.time + 0.25f / __instance.accelerationOutput.Factor;
			__instance.accelerationOutput.OnTriggered();

			return false;
		}
	}

	[HarmonyPatch(typeof(WeaponVacuum), "Update")]
	static class WeaponVacuum_Update_Patch
	{
		static bool Prefix(WeaponVacuum __instance)
		{
			if (Time.timeScale == 0f)
				return false;

			HashSet<GameObject> inVac = __instance.tracker.CurrColliders();
			__instance.UpdateHud(inVac);
			__instance.UpdateSlotForInputs();
			__instance.UpdateVacModeForInputs();

			SRSingleton<SceneContext>.Instance.PlayerState.InGadgetMode = (__instance.vacMode == WeaponVacuum.VacMode.GADGET);
			if (SRInput.Actions.attack.WasPressed || SRInput.Actions.vac.WasPressed || SRInput.Actions.burst.WasPressed)
				__instance.launchedHeld = false;

			float num = 1f;
			if (Time.fixedTime >= __instance.nextShot && !__instance.launchedHeld && __instance.vacMode == WeaponVacuum.VacMode.SHOOT)
			{
				__instance.Expel(inVac);
				num = __instance.GetShootSpeedFactor(inVac);
				__instance.nextShot = Time.fixedTime + __instance.shootCooldown / num;
			}

			if (__instance.vacAnimator != null)
				__instance.vacAnimator.speed = num;

			if (!__instance.launchedHeld && __instance.vacMode == WeaponVacuum.VacMode.VAC)
			{
				__instance.vacAudioHandler.SetActive(true);
				__instance.vacFX.SetActive(__instance.held == null);
				__instance.siloActivator.enabled = (__instance.held == null);

				if (__instance.held != null)
					__instance.UpdateHeld(inVac);
				else
					__instance.Consume(inVac);
			}
			else
			{
				__instance.ClearVac();
			}

			__instance.UpdateVacAnimators();

			return false;
		}
	}

#if DEBUG
	static class DebugPatches
	{
		[HarmonyPatch(typeof(WeaponVacuum), "Start")]
		static class VacpackAxisPatch
		{
			static bool Prepare() => Config.Dbg.showColliders;

			static void Prefix(WeaponVacuum __instance) =>
				__instance.vacOrigin.ensureComponent<DrawAxis>().scale = Vector3.one * 10f;
		}

		[HarmonyPatch(typeof(SiloStorage), "OnAdded")]
		static class SiloStorageColliderPatch
		{
			static bool Prepare() => Config.Dbg.showColliders;

			static void Prefix(SiloStorage __instance) =>
				__instance.gameObject.ensureComponent<DrawColliders>();
		}

		[HarmonyPatch(typeof(PlayerState), "Update")]
		static class MoneyCheatPatch
		{
			const int moneyStep = 10000;
			const int moneyMax = 1000000;

			static bool Prepare() => Config.Dbg.moneyCheat;

			static void Postfix(PlayerState __instance)
			{
				if (__instance.model.currency < moneyMax)
					__instance.AddCurrency(moneyStep);
			}
		}
	}
#endif // DEBUG
}