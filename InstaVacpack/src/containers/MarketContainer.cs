namespace InstaVacpack
{
	// wrapper around plort market and market link
	class MarketContainer: IItemContainer
	{
		readonly ScorePlort scorePlort;

		public MarketContainer(ScorePlort scorePlort, Identifiable.Id id)
		{
			this.id = id;
			this.scorePlort = scorePlort;
		}

		public Identifiable.Id id { get; }

		public int count => 0;
		public int maxCount => int.MaxValue;

		public bool canAdd => scorePlort.CanDeposit(id);
		public bool canRemove => false;

		public bool add(int count) => scorePlort.Deposit(id, count)?.deposits > 0;
		public bool remove(int _) => false;
	}
}