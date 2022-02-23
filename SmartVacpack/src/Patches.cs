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

	// show additional info about selected item in vacpack (market price and amount in refinery)
	[HarmonyPatch(typeof(TargetingUI), "Update")]
	static class TargetingUI_Update_Patch
	{
		static bool Prepare() => Config.showAdditionalInfo;

		static void Postfix(TargetingUI __instance)
		{
			if (!__instance.player.Targeting)
				return;

			var selectedAmmo = __instance.player.Ammo.GetSelectedId();

			if (selectedAmmo == Identifiable.Id.NONE)
				return;

			// can't use 'player.Targeting', need to use interactable layer
			if (Common.Vacpack.Utils.tryGetPointedObject() is not GameObject go)
				return;

			if (go.TryGetComponent<ScorePlort>(out var scorePlort))
			{
				if (scorePlort.GetMarketValue(selectedAmmo, false) is int price)
					_showInfo($"Price: {price}");
			}
			else if (go.GetComponent<SiloCatcher>()?.type == SiloCatcher.Type.REFINERY)
			{
				if (!GadgetDirector.IsRefineryResource(selectedAmmo))
					return;

				int amount = SRSingleton<SceneContext>.Instance.GadgetDirector.GetRefineryCount(selectedAmmo);
				_showInfo($"Amount: {amount}");
			}

			void _showInfo(string info)
			{
				__instance.nameText.enabled = __instance.infoText.enabled = true;
				__instance.nameText.text = Identifiable.GetName(selectedAmmo);
				__instance.infoText.text = info;
			}
		}
	}

	// allows to switch silo slots manually remotely
	[HarmonyPatch(typeof(WeaponVacuum), "Update")]
	static class WeaponVacuum_Update_Patch
	{
		static bool Prepare() => Config.allowToSwitchSiloSlotsManually;

		static void Postfix(WeaponVacuum __instance)
		{
			if (!SRInput.Actions.interact.WasReleased)
				return;

			if (Common.Vacpack.Utils.tryGetPointedObject(__instance, false) is not GameObject go)
				return;

			var slotSwitcher = go.GetComponent<SiloStorageActivator>() ?? go.GetComponent<SiloCatcher>()?.getActivator();
			slotSwitcher?.Activate();
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

	// highlighting for selected slot
	static class SlotHighlightPatches
	{
		static Color highlightColor =
			ColorUtility.TryParseHtmlString(Config.highlightColor, out var color)? color: Color.white;

		[HarmonyPatch(typeof(AmmoSlotUI), "Start")]
		static class AmmoSlotUI_Start_Patch
		{
			static bool Prepare() => Config.highlightSelectedSlot;

			static void Postfix(AmmoSlotUI __instance) =>
				__instance.selected.GetComponent<UnityEngine.UI.Image>().color = highlightColor;
		}

		[HarmonyPatch(typeof(AmmoSlotUI), "Update")]
		static class AmmoSlotUI_Update_Patch
		{
			static bool Prepare() => Config.highlightSelectedSlot;

			static void changeSelectedSlotColor(AmmoSlotUI slotUI, int selectedSlot)
			{																									$"AmmoSlotUI_Update_Patch: selected slot changed (previous: {slotUI.lastSelectedAmmoIndex}, currrent: {selectedSlot})".logDbg();
				if (slotUI.lastSelectedAmmoIndex != -1)
					slotUI.slots[slotUI.lastSelectedAmmoIndex].front.color = Color.white;

				slotUI.slots[selectedSlot].front.color = highlightColor;
			}

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
			{
				// insert call to 'changeSelectedSlotColor' inside block 'if (selectedAmmoIdx == i)'
				return cins.ciInsert(new CIHelper.MemberMatch("SetParent"),
					OpCodes.Ldarg_0,
					OpCodes.Ldloc_1,
					CIHelper.emitCall<Action<AmmoSlotUI, int>>(changeSelectedSlotColor));
			}
		}
	}
}