using System;

namespace InstaVacpack
{
	// wrapper around vacpack ammo
	class PlayerAmmoContainer: AmmoContainer
	{
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

		// get usable slot for specific items
		// allows to explicitly use (slot needs to be selected) multiple slots for the same item type
		int getBestSlot(Identifiable.Id id)
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
	}
}