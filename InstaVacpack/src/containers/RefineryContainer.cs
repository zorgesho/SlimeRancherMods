namespace InstaVacpack
{
	// wrapper around refinery and refinery link
	class RefineryContainer: IItemContainer
	{
		public RefineryContainer(Identifiable.Id id) => this.id = id;

		public Identifiable.Id id { get; }

		public int count => SRSingleton<SceneContext>.Instance.GadgetDirector.GetRefineryCount(id);
		public int maxCount => GadgetDirector.REFINERY_MAX;

		public bool canAdd => GadgetDirector.IsRefineryResource(id) && count < maxCount;
		public bool canRemove => false;

		public bool add(int count) => SRSingleton<SceneContext>.Instance.GadgetDirector.AddToRefinery(id, count) > 0;
		public bool remove(int _) => false;
	}
}