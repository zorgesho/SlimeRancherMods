using System;
using System.Linq;

namespace InstaVacpack
{
	// wrapper around vacpack ammo
	class PlayerAmmoContainer: AmmoContainer
	{
		static readonly bool allowSameMultipleSlots =
			(bool)(Type.GetType("SmartVacpack.Config, SmartVacpack")?.GetField("sameMultipleSlots")?.GetValue(null) ?? false);

		// if 'id' is NONE we will use currently selected ammo
		public PlayerAmmoContainer(Identifiable.Id id = Identifiable.Id.NONE)
		{
			ammo = SRSingleton<SceneContext>.Instance.PlayerState.Ammo;

			if (id == Identifiable.Id.NONE) // using selected slot
			{
				slotIndex = ammo.selectedAmmoIdx;
				this.id = ammo.Slots[slotIndex]?.id ?? Identifiable.Id.NONE;
			}
			else // trying to choose best slot
			{
				slotIndex = getBestSlot(id);

				if (slotIndex != -1)
					this.id = id;
				else
					valid = false;
			}
		}

		int getBestSlot(Identifiable.Id id)
		{
			if (!ammo.CouldAddToSlot(id))
				return -1;

			int selectedSlotIdx = ammo.GetSelectedAmmoIdx();

			if (ammo.GetSlotName(selectedSlotIdx) == id && ammo.CouldAddToSlot(id, selectedSlotIdx, false))
				return selectedSlotIdx;

			if (!allowSameMultipleSlots && ammo.GetAmmoIdx(id) is int idx)
				return ammo.GetSlotCount(idx) < ammo.GetSlotMaxCount(idx)? idx: -1;

			return Enumerable.Range(0, ammo.GetUsableSlotCount()).Where(i => ammo.CouldAddToSlot(id, i, false)).DefaultIfEmpty(-1).FirstOrDefault();
		}
	}
}