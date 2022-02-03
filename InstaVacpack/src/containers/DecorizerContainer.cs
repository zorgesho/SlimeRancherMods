using System.Linq;
using MonomiPark.SlimeRancher.DataModel;

namespace InstaVacpack
{
	// wrapper around decorizer
	class DecorizerContainer: IItemContainer
	{
		readonly bool valid = false;
		readonly DecorizerStorage storage;

		public DecorizerContainer(SiloCatcher siloCatcher, Identifiable.Id id)
		{
			if (id != Identifiable.Id.NONE && !DecorizerModel.ITEM_CLASSES.Any(c => c.Contains(id)))
				return;

			valid = true;
			storage = siloCatcher.storageDecorizer;
			this.id = id != Identifiable.Id.NONE? id: storage.selected;
		}

		public Identifiable.Id id { get; }

		public int count => valid? storage.model.GetCount(id): -1;
		public int maxCount => valid? int.MaxValue: -1;

		public bool canAdd => valid;
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