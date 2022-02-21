using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common;
using Common.Harmony;

namespace SmartVacpack
{
	static class CommonPatches
	{
		public static void init()
		{
			Common.Vacpack.Patches.setModeHandlers(handleShootMode, handleVacMode, handleExit);
			AnimHandler.init();
		}

		static class AnimHandler
		{
			const int effectGapFrames = 30;
			static int frameEffectPlayed = 0;

			static bool disableAnims = false;
			public static void disableForFrame() => disableAnims = true;

			public static void init()
			{
				static bool _overrideAnims() => FlyingItems.isAnyItem();
				Common.Vacpack.Patches.setAnimHandlers(handleAnims, _overrideAnims);
			}

			static bool handleAnims(WeaponVacuum vac)
			{
				bool disabled = disableAnims && !FlyingItems.isAnyItem();
				disableAnims = false;

				if (disabled && Common.Vacpack.Utils.frameVacModeChanged == Time.frameCount && frameEffectPlayed < Time.frameCount - effectGapFrames)
				{
					vac.CaptureFailedEffect();
					frameEffectPlayed = Time.frameCount;
				}

				return !disabled;
			}
		}

		// disabling vacpack in case current action is restricted, switching silo slots if necessary
		static bool handleShootMode(WeaponVacuum vac)
		{
			if (vac.held || vac.vacMode != WeaponVacuum.VacMode.SHOOT)
				return true;

			var id = vac.player.Ammo.GetSelectedId();

			if (Common.Vacpack.Utils.tryGetPointedObject<SiloCatcher>(vac) is not SiloCatcher silo)
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

		static bool handleVacMode(WeaponVacuum vac)
		{
			if (Common.Vacpack.Utils.tryGetPointedObject<SiloCatcher>(vac) is SiloCatcher silo)
				return handleVacMode(vac, silo);

			return true;
		}

		// disabling vacpack in case current action is restricted
		public static bool handleVacMode(WeaponVacuum vac, SiloCatcher silo)
		{
			if (!silo.hasSiloStorage())
				return true;

			var playerAmmo = vac.player.Ammo;
			var id = silo.storageSilo.GetRelevantAmmo().GetSlotName(silo.slotIdx);

			if (!Config.sameMultipleSlots)
				return playerAmmo.CouldAddToSlot(id) && playerAmmo.GetCount(id) < vac.player.model.maxAmmo - FlyingItems.vacItemsCount;

			return Enumerable.Range(0, playerAmmo.GetUsableSlotCount()).Any(i => playerAmmo.couldAddToSlot(id, i, FlyingItems.vacItemsCount + 1));
		}

		static void handleExit(bool result)
		{
			if (!result)
				AnimHandler.disableForFrame();
		}
	}

	// allows to use multiple slots for the same type of items
	[HarmonyPatch(typeof(Ammo), "MaybeAddToSlot")]
	static class Ammo_MaybeAddToSlot_Patch
	{
		static bool Prepare() => Config.sameMultipleSlots;

		static int lastUsedSlot = -1;
		public static int getLastUsedSlot() => lastUsedSlot;

		static void setLastUsedSlot(int slot)
		{																									$"Ammo_MaybeAddToSlot_Patch: last used slot is {slot}".logDbg();
			lastUsedSlot = slot;
		}

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
			list.ciInsert(i[2], OpCodes.Br, list[i[1]].operand);

			// saving used slot for SRML patch
			list.ciInsert(new CIHelper.MemberMatch("Min"), OpCodes.Ldloc_3, CIHelper.emitCall<Action<int>>(setLastUsedSlot));
			list.ciInsert(ci => ci.isOp(OpCodes.Newobj), +0, 2, OpCodes.Ldloc_S, 6, CIHelper.emitCall<Action<int>>(setLastUsedSlot));

			return list;
		}
	}

	// compatibility patch, fixes the slot that used for saving extended data
	[HarmonyPatch(typeof(SRML.SR.SaveSystem.Patches.AmmoMaybeAddToSlotPatch), "Postfix")]
	static class SRML_AmmoMaybeAddToSlotPatch_Postfix_Patch
	{
		static bool Prepare() => Config.sameMultipleSlots;

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
		{
			// insert right before 'if (count == -1)' check
			return cins.ciInsert(ci => ci.isOp(OpCodes.Ldloc_3), +0, 1,
				CIHelper.emitCall<Func<int>>(Ammo_MaybeAddToSlot_Patch.getLastUsedSlot),
				OpCodes.Stloc_3);
		}
	}

	// In case we trying to use the storage, but raycast in WeaponVacuum_Update_Patch misses the silo catcher.
	// Vacpack anims will be played, but items will stay in the storage.
	[HarmonyPatch(typeof(SiloCatcher), "Remove")]
	static class SiloCatcher_Remove_Patch
	{
		static bool Prefix(SiloCatcher __instance, ref bool __result) =>
			__result = CommonPatches.handleVacMode(__instance.vac, __instance);
	}
}