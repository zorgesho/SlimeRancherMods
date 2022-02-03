using Common;

namespace InstaVacpack
{
	abstract class AmmoContainer: IItemContainer
	{
		protected bool valid { get; init; } = true;

		protected Ammo ammo { get; init; }
		protected int slotIndex { get; init; }

		public Identifiable.Id id { get; init; }

		public virtual int count => valid? ammo.GetSlotCount(slotIndex): -1;
		public virtual int maxCount => valid? ammo.GetSlotMaxCount(slotIndex): -1;

		public bool canAdd => valid && count < maxCount;
		public bool canRemove => valid && count > 0;

		public virtual bool add(int count)
		{																										$"Trying to add {count} of {id} to {this} (canAdd is {canAdd})".logDbg();
			if (!canAdd)
				return false;

			return ammo.MaybeAddToSpecificSlot(id, null, slotIndex, count);
		}

		public virtual bool remove(int count)
		{																										$"Trying to remove {count} of {id} from {this} (canRemove is {canRemove})".logDbg();
			if (!canRemove)
				return false;

			ammo.Decrement(slotIndex, count);
			return true;
		}
	}
}