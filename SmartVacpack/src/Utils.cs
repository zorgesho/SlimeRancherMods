using System;
using System.Linq;

using Common;

namespace SmartVacpack
{
	static class Utils
	{
		public static bool couldAddToSlot(this Ammo ammo, Identifiable.Id id, int slotIdx, int count = 1)
		{
			if (!ammo.CouldAddToSlot(id, slotIdx, false))
				return false;

			return ammo.GetSlotMaxCount(slotIdx) - ammo.GetSlotCount(slotIdx) >= count;
		}

		public static SiloStorageActivator getActivator(this SiloCatcher siloCatcher)
		{
			return siloCatcher?.gameObject.getChild("../../techActivator/triggerActivate")?.GetComponent<SiloStorageActivator>();
		}

		public static bool hasSiloStorage(this SiloCatcher siloCatcher)
		{
			return siloCatcher.type is SiloCatcher.Type.SILO_DEFAULT or SiloCatcher.Type.SILO_OUTPUT_ONLY;
		}

		public static int[] getSlots(this SiloStorageActivator siloActivator)
		{
			return siloActivator.siloSlotUIs.Select(ui => ui.slotIdx).ToArray();
		}

		public static bool setSlot(this SiloStorageActivator siloActivator, int slotIdx)
		{
			int idx = Array.FindIndex(siloActivator.getSlots(), i => i == slotIdx);

			if (idx == -1)
				return false;

			siloActivator.landPlotModel.siloStorageIndices[siloActivator.activatorIdx] = idx;
			siloActivator.OnActiveSlotChanged();
			return true;
		}

		///<summary>Get best possible slot in the <paramref name="ammo"/> for <paramref name="id"/></summary>
		///<param name="multiSame">Allow to use multiple slots for the same <paramref name="id"/></param>
		///<param name="priorityReuse">Prefer using slots with <paramref name="id"/> over the empty slots</param>
		///<param name="slots">Use only these slots when choose (use all slots by default)</param>
		///<param name="selectedSlotIdx">use this slot as selected (use selected slot from <paramref name="ammo"/> by default)</param>
		public static int getBestSlot(Ammo ammo, Identifiable.Id id, bool multiSame = true, bool priorityReuse = false, int[] slots = null, int? selectedSlotIdx = null)
		{
			bool _slotHasFreeSpace(int slotIdx) => ammo.GetSlotCount(slotIdx) < ammo.GetSlotMaxCount(slotIdx);

			int _getAmmoIdx(Identifiable.Id id, bool checkFreeSpace = false)
			{
				for (int i = 0; i < (slots?.Length ?? ammo.GetUsableSlotCount()); i++)
				{
					int idx = slots?[i] ?? i;

					if ((ammo.Slots[idx]?.id ?? Identifiable.Id.NONE) == id && (!checkFreeSpace || _slotHasFreeSpace(idx)))
						return idx;
				}

				return -1;
			}

			int ammoIdx = _getAmmoIdx(id);
			int ammoIdxFree = _getAmmoIdx(id, true);

			int slotIdx = selectedSlotIdx ?? ammo.GetSelectedAmmoIdx();
			var slot = ammo.Slots[slotIdx];

			if (slot == null)
			{
				if (priorityReuse && ammoIdxFree != -1)
					return ammoIdxFree;

				if (multiSame || ammoIdx == -1)
					return slotIdx;
			}
			else if (slot.id == id)
			{
				if (_slotHasFreeSpace(slotIdx))
					return slotIdx;
			}

			if (ammoIdxFree != -1)
				return ammoIdxFree;

			if (multiSame || ammoIdx == -1)
				return _getAmmoIdx(Identifiable.Id.NONE);

			return -1;
		}
	}
}