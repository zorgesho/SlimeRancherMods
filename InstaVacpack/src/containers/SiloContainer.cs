namespace InstaVacpack
{
	// wrapper around silo and auto-feeder
	class SiloContainer: AmmoContainer
	{
		readonly SiloStorage storage;

		public SiloContainer(SiloCatcher siloCatcher, Identifiable.Id id)
		{
			valid = false;

			storage = siloCatcher.storageSilo;
			ammo = storage.GetRelevantAmmo();
			slotIndex = siloCatcher.slotIdx;

			if (slotIndex >= ammo.ammoModel.usableSlots) // just in case
				return;

			if (id != Identifiable.Id.NONE && !ammo.potentialAmmo.Contains(id))
				return;

			var slotId = ammo.Slots[slotIndex]?.id ?? Identifiable.Id.NONE;

			if (id == Identifiable.Id.NONE) // using selected slot
				this.id = slotId;
			else if (slotId == Identifiable.Id.NONE || slotId == id)
				this.id = id;
			else
				return;

			valid = true;
		}

		public override bool add(int count)
		{
			bool result = base.add(count);

			if (result)
				storage.OnAdded();

			return result;
		}
	}

	// wrapper around plort collector (output only)
	class SiloOutputContainer: SiloContainer
	{
		public override int maxCount => 0;

		public SiloOutputContainer(SiloCatcher siloCatcher, Identifiable.Id id): base(siloCatcher, id) {}
	}
}