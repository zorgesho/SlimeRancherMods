namespace InstaVacpack
{
	// wrapper around Viktor's storage
	class ViktorContainer: IItemContainer
	{
		readonly bool valid = false;
		readonly GlitchStorage storage;

		public ViktorContainer(SiloCatcher siloCatcher, Identifiable.Id id)
		{
			if (id != Identifiable.Id.NONE && !SiloStorage.StorageType.NON_SLIMES.Contains(id))
				return;

			storage = siloCatcher.storageGlitch;

			if (id == Identifiable.Id.NONE)
				this.id = storage.model.id;
			else if (storage.model.id == Identifiable.Id.NONE || id == storage.model.id)
				this.id = id;
			else
				return;

			valid = true;
		}

		public Identifiable.Id id { get; }

		public int count => valid? storage.count: -1;
		public int maxCount => valid? GlitchStorage.MAX_COUNT: -1;

		public bool canAdd => count < maxCount;
		public bool canRemove => count > 0;

		public bool add(int count)
		{
			if (!canAdd)
				return false;

			return Utils.runMultiple(() => storage.Add(id), count);
		}

		public bool remove(int count)
		{
			if (!canRemove)
				return false;

			return Utils.runMultiple(() => storage.Remove(out _), count);
		}
	}
}