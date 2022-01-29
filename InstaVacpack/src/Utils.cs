using System;
using UnityEngine;
using Common;

namespace InstaVacpack
{
	static class Utils
	{
		public static Ammo playerAmmo => SRSingleton<SceneContext>.Instance.PlayerState.Ammo;

		public static Ammo.Slot getSelectedSlot(this SiloCatcher siloCatcher)
		{
			if (siloCatcher.type != SiloCatcher.Type.SILO_DEFAULT)
				return null;

			var ammo = siloCatcher.storageSilo.GetRelevantAmmo();
			return siloCatcher.slotIdx < ammo.ammoModel.usableSlots? ammo.Slots[siloCatcher.slotIdx]: null;
		}

		public static bool tryGetPointedSilo(WeaponVacuum vacpack, out SiloCatcher silo, float distance = Mathf.Infinity) // TODO distance ?
		{
			var tr = vacpack.vacOrigin.transform;
			Physics.Raycast(new Ray(tr.position, tr.up), out RaycastHit hit, distance, 1 << vp_Layer.Interactable, QueryTriggerInteraction.Collide);

			silo = hit.collider?.GetComponent<SiloCatcher>();
			return silo != null;
		}

		// get usable slot for specific items
		// allows to explicitly use (slot needs to be selected) multiple slots for the same item type
		public static int getUsableSlot(Ammo ammo, Identifiable.Id id)
		{
			// try to use selected slot first
			int selectedSlotIdx = ammo.GetSelectedAmmoIdx();
			var selectedSlot = ammo.Slots[selectedSlotIdx];

			// if selected slot is empty then use it without checking other slots
			if (selectedSlot == null)
				return selectedSlotIdx;

			// if selected slot contains correct items then try to use it if it has space, without checking other slots
			if (selectedSlot.id == id)
				return selectedSlot.count < ammo.GetSlotMaxCount(selectedSlotIdx)? selectedSlotIdx: -1;

			// if some nonselected slot contains correct items then try to use it if it has space, without checking other slots
			int ammoIdx = ammo.GetAmmoIdx(id) ?? -1;
			if (ammoIdx != -1)
				return ammo.CouldAddToSlot(id, ammoIdx, false)? ammoIdx: -1;

			// if there is no suitable slot, use first empty slot, if any
			return Array.FindIndex(ammo.Slots, slot => slot == null);
		}

		public static bool tryTransferMaxAmount(Ammo sourceAmmo, int sourceSlotIdx, Ammo targetAmmo, int targetSlotIdx)
		{																												$"tryTransferMaxAmount: source slot = {sourceSlotIdx}, target slot = {targetSlotIdx}".logDbg();
			static bool _checkParams(Ammo ammo, int slotIndex) =>
				ammo != null && slotIndex >= 0 && slotIndex < ammo.GetUsableSlotCount();

			Common.Debug.assert(sourceAmmo != null && targetAmmo != null);

			if (!_checkParams(sourceAmmo, sourceSlotIdx) || !_checkParams(targetAmmo, targetSlotIdx))
				return false;

			var sourceSlot = sourceAmmo.Slots[sourceSlotIdx];

			if (sourceSlot == null)
			{																											"tryTransferMaxAmount: source slot is null!".logDbg();
				return false;
			}

			if (!targetAmmo.CouldAddToSlot(sourceSlot.id, targetSlotIdx, false))
			{																											$"tryTransferMaxAmount: can't add {sourceSlot.id} to target slot".logDbg();
				return false;
			}

			var targetSlot = targetAmmo.Slots[targetSlotIdx];

			int freeSpaceInTarget = targetAmmo.GetSlotMaxCount(targetSlotIdx) - (targetSlot?.count ?? 0);
			int countToTransfer = Math.Min(sourceSlot.count, freeSpaceInTarget);

			targetAmmo.MaybeAddToSpecificSlot(sourceSlot.id, null, targetSlotIdx, countToTransfer, false); // null identifiable
			sourceAmmo.Decrement(sourceSlotIdx, countToTransfer);

			return true;
		}
	}
}