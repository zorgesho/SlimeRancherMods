using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common.Harmony;

namespace SmartVacpack
{
	// We process shoot/vac modes here.
	// Disable vacpack in case current action is impossible, switch silo slots if necessary.
	[HarmonyPatch(typeof(WeaponVacuum), "Update")]
	static class WeaponVacuum_Update_Patch
	{
		static bool processShootMode(WeaponVacuum vac)
		{
			if (vac.vacMode != WeaponVacuum.VacMode.SHOOT)
				return true;

			var id = vac.player.Ammo.GetSelectedId();

			if (id == Identifiable.Id.NONE)
				return false;

			if (Utils.tryGetPointedObject(vac)?.GetComponent<SiloCatcher>() is not SiloCatcher silo)
				return true;

			if (!silo.hasSiloStorage())
				return true;

			var ammo = silo.storageSilo.GetRelevantAmmo();
			bool couldUseCurrentSlot = ammo.couldAddToSlot(id, silo.slotIdx, FlyingItems.expItemsCount + 1);

			if (couldUseCurrentSlot && (Config.preferEmptySlots || ammo.GetSlotCount(silo.slotIdx) > 0))
				return true;

			if (silo.getActivator() is not SiloStorageActivator storageActivator)
				return couldUseCurrentSlot;

			int bestSlot = Utils.getBestSlot(ammo, id, true, !Config.preferEmptySlots, storageActivator.getSlots(), silo.slotIdx);

			if (bestSlot == -1 || (silo.slotIdx == bestSlot && ammo.GetSlotName(bestSlot) != Identifiable.Id.NONE))
				return false;

			storageActivator.setSlot(bestSlot);
			return true;
		}

		static bool processVacMode(WeaponVacuum vac)
		{
			if (Utils.tryGetPointedObject(vac)?.GetComponent<SiloCatcher>() is SiloCatcher silo)
				return processVacMode(vac, silo);

			return true;
		}

		public static bool processVacMode(WeaponVacuum vac, SiloCatcher silo)
		{
			if (!silo.hasSiloStorage())
				return true;

			var playerAmmo = vac.player.Ammo;
			var id = silo.storageSilo.GetRelevantAmmo().GetSlotName(silo.slotIdx);
			bool isSlotAvailable = playerAmmo.CouldAddToSlot(id);

			if (!Config.sameMultipleSlots)
				return isSlotAvailable && playerAmmo.GetCount(id) < vac.player.model.maxAmmo - FlyingItems.vacItemsCount;

			return Enumerable.Range(0, playerAmmo.GetUsableSlotCount()).Any(i => playerAmmo.couldAddToSlot(id, i, FlyingItems.vacItemsCount + 1));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins, ILGenerator ilg)
		{
			var list = cins.ToList();
			var actionEnabled = ilg.DeclareLocal(typeof(bool));

			var label1 = ilg.DefineLabel();
			var label2 = ilg.DefineLabel();

			var call_Expel = new CIHelper.MemberMatch(nameof(WeaponVacuum.Expel));
			var call_SetActive = new CIHelper.MemberMatch("SetActive");

			// inserting call to 'processShootMode' _before_ line with 'nextShot' check (so we can call it on each frame)
			list.ciInsert(ci => ci.isLDC(1f), +2, 1,
				OpCodes.Ldarg_0,
				CIHelper.emitCall<Func<WeaponVacuum, bool>>(processShootMode),
				OpCodes.Stloc, actionEnabled);

			// don't call 'Expel' if current action is disabled
			list.ciInsert(call_Expel, -2, 1, OpCodes.Ldloc, actionEnabled, OpCodes.Brfalse, label1);
			list.ciInsert(call_Expel, CIHelper.emitLabel(label1));

			// inserting call to 'processVacMode' after 'vacMode == VacMode.VAC'
			list.ciInsert(ci => ci.isOp(OpCodes.Ldc_I4_2), +2, 1,
				OpCodes.Ldarg_0,
				CIHelper.emitCall<Func<WeaponVacuum, bool>>(processVacMode),
				OpCodes.Stloc, actionEnabled);

			int[] ii = list.ciFindIndexes(
				ci => ci.isOp(OpCodes.Ldc_I4_2), // vacMode == VacMode.VAC
				call_SetActive, // this.vacAudioHandler.SetActive(true);
				call_SetActive, // this.vacFX.SetActive(this.held == null);
				new CIHelper.MemberMatch("set_enabled")); // this.siloActivator.enabled = (this.held == null);

			if (ii == null)
				return cins;

			// disabling various stuff in 'VacMode.VAC' block if current action is disabled
			for (int i = 3; i > 0; i--)
			{
				var label = ilg.DefineLabel();

				list.ciInsert(ii[i],
					OpCodes.Ldloc, actionEnabled,
					OpCodes.Brtrue, label,
					OpCodes.Pop,
					i == 3? OpCodes.Ldc_I4_0: CIHelper.emitCall<Func<bool>>(FlyingItems.isAnyItem),
					CIHelper.emitLabel(label));
			}

			// disable vacpack animations for this frame if current action is disabled
			list.ciInsert(new CIHelper.MemberMatch(nameof(WeaponVacuum.UpdateVacAnimators)), 0, 1,
				OpCodes.Ldloc, actionEnabled,
				OpCodes.Brtrue, label2,
				CIHelper.emitCall<Action>(WeaponVacuum_UpdateVacAnimators_Patch.disableAnimsForFrame),
				CIHelper.emitLabel(label2));

			return list;
		}
	}

	// correctly disabling animations for one frame if current action is disabled (and playing fail fx if necessary)
	[HarmonyPatch(typeof(WeaponVacuum), "UpdateVacAnimators")]
	static class WeaponVacuum_UpdateVacAnimators_Patch
	{
		const int effectGapFrames = 30;
		static int frameEffectPlayed = 0;

		static bool disableAnims = false;
		public static void disableAnimsForFrame() => disableAnims = true;

		static bool updateAnims(WeaponVacuum vac)
		{
			bool disabled = disableAnims && !FlyingItems.isAnyItem();
			disableAnims = false;

			if (disabled && Utils.frameVacModeChanged == Time.frameCount && frameEffectPlayed < Time.frameCount - effectGapFrames)
			{
				vac.CaptureFailedEffect();
				frameEffectPlayed = Time.frameCount;
			}

			return !disabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins, ILGenerator ilg)
		{
			var label = ilg.DefineLabel();

			// insert right after assigning all the flags
			return cins.ciInsert(ci => ci.isOp(OpCodes.Stloc_2),
				OpCodes.Ldarg_0,
				CIHelper.emitCall<Func<WeaponVacuum, bool>>(updateAnims),
				OpCodes.Brtrue, label,
				OpCodes.Ldloc_0, OpCodes.Stloc_1,	// flag2 = flag;
				OpCodes.Ldc_I4_0, OpCodes.Stloc_2,	// flag3 = false;
				CIHelper.emitLabel(label));
		}
	}

	// allows to use multiple slots for the same type of items
	[HarmonyPatch(typeof(Ammo), "MaybeAddToSlot")]
	static class Ammo_MaybeAddToSlot_Patch
	{
		static bool Prepare() => Config.sameMultipleSlots;

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
		{
			var list = cins.ToList();

			int[] i = list.ciFindIndexes(
				OpCodes.Br,
				OpCodes.Brfalse,   // label for 'continue'
				OpCodes.Ldc_I4_1); // flag3 = true;

			if (i == null)
				return cins;

			// adding 'continue' right before 'flag3 = true;'
			return list.ciInsert(i[2], OpCodes.Br, list[i[1]].operand);
		}
	}

	// In case we trying to use the storage, but raycast in WeaponVacuum_Update_Patch misses the silo catcher.
	// Vacpack anims will be played, but items will stay in the storage.
	[HarmonyPatch(typeof(SiloCatcher), "Remove")]
	static class SiloCatcher_Remove_Patch
	{
		static bool Prefix(SiloCatcher __instance, ref bool __result) =>
			__result = WeaponVacuum_Update_Patch.processVacMode(__instance.vac, __instance);
	}
}