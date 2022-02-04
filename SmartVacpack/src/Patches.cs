using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

namespace SmartVacpack
{
	[HarmonyPatch(typeof(WeaponVacuum), "Update")]
	static class WeaponVacuum_Update_Patch
	{
		static GameObject tryGetPointedObject(WeaponVacuum vacpack, float distance = Mathf.Infinity)
		{
			var tr = vacpack.vacOrigin.transform;
			Physics.Raycast(new Ray(tr.position, tr.up), out RaycastHit hit, distance, 1 << vp_Layer.Interactable, QueryTriggerInteraction.Collide);

			return hit.collider?.gameObject;
		}

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

			//// added code: begin
			bool actionDisabled = false;

			if (__instance.vacMode == WeaponVacuum.VacMode.SHOOT)
			{
				if (tryGetPointedObject(__instance)?.GetComponent<SiloCatcher>() is not SiloCatcher silo)
					return true;

				var id = __instance.player.Ammo.GetSelectedId();
				var ammo = silo.storageSilo.GetRelevantAmmo();

				actionDisabled = !ammo.CouldAddToSlot(id, silo.slotIdx, false); // TODO consider already flying too
			}
			//// added code: end

			if (Time.fixedTime >= __instance.nextShot && !__instance.launchedHeld && __instance.vacMode == WeaponVacuum.VacMode.SHOOT)
			{
				if (!actionDisabled) // <<< added
					__instance.Expel(inVac);

				num = __instance.GetShootSpeedFactor(inVac);
				__instance.nextShot = Time.fixedTime + __instance.shootCooldown / num;
			}

			if (__instance.vacAnimator != null)
				__instance.vacAnimator.speed = num;

			if (!__instance.launchedHeld && __instance.vacMode == WeaponVacuum.VacMode.VAC)
			{
				//// added code: begin
				if (tryGetPointedObject(__instance)?.GetComponent<SiloCatcher>() is not SiloCatcher silo)
					return true;

				var ammo = silo.storageSilo.GetRelevantAmmo();

				if (silo.slotIdx < ammo.ammoModel.usableSlots)
				{
					var slot = ammo.Slots[silo.slotIdx];
					var id = ammo.AdjustId(slot.id);

					if (__instance.player.Ammo.GetCount(id) == 100) // TODO actual max count and consider flying items
						actionDisabled = true;
				}
				//// added code: end

				if (!actionDisabled) // <<< added
				{
					__instance.vacAudioHandler.SetActive(true);
					__instance.vacFX.SetActive(__instance.held == null);
					__instance.siloActivator.enabled = (__instance.held == null);
				}
				else // <<< added
				{
					__instance.vacAudioHandler.SetActive(false);
					__instance.vacFX.SetActive(false);
					__instance.siloActivator.enabled = false;
				}

				if (__instance.held != null)
					__instance.UpdateHeld(inVac);
				else
					__instance.Consume(inVac);
			}
			else
			{
				__instance.ClearVac();
			}

			if (actionDisabled) // <<< added
				WeaponVacuum_UpdateVacAnimators_Patch.disableAnimsForFrame();

			__instance.UpdateVacAnimators();

			return false;
		}
	}

	[HarmonyPatch(typeof(WeaponVacuum), "UpdateVacAnimators")]
	static class WeaponVacuum_UpdateVacAnimators_Patch
	{
		static bool disableAnims = false;

		public static void disableAnimsForFrame() => disableAnims = true;
		static bool isDisabled() => disableAnims && !(disableAnims = false);

		static bool Prefix(WeaponVacuum __instance)
		{
			bool flag = __instance.playerModel.hasAirBurst && SRInput.Actions.burst.WasPressed;
			bool flag2 = __instance.vacMode == WeaponVacuum.VacMode.SHOOT || __instance.vacMode == WeaponVacuum.VacMode.VAC || flag;
			bool flag3 = __instance.vacMode == WeaponVacuum.VacMode.VAC;

			if (isDisabled()) // <<< added
			{
				flag2 = flag;
				flag3 = false;
			}

			if (__instance.vacAnimator == null)
			{
				__instance.vacAnimator = __instance.GetComponentInChildren<Animator>();
				__instance.vacColorAnimator = __instance.GetComponentInChildren<VacColorAnimator>();
			}

			__instance.vacAnimator.SetBool(__instance.animActiveId, flag2);
			__instance.vacAnimator.SetBool(__instance.animVacModeId, flag3);
			__instance.vacAnimator.SetBool(__instance.animHoldingId, __instance.held != null);
			__instance.vacColorAnimator.SetVacActive(flag2);
			__instance.vacColorAnimator.SetVacMode(flag3);

			if (flag)
				__instance.AirBurst();

			__instance.vacAnimator.SetBool(__instance.animSprintingId, __instance.playerEvents.Run.Active);

			return false;
		}
	}
}