namespace InstaVacpack
{
	// wrapper interface for simplifying item transfer
	interface IItemContainer
	{
		Identifiable.Id id { get; }

		int count { get; }
		int maxCount { get; }

		bool canAdd { get ; }
		bool canRemove { get; }

		bool add(int count);
		bool remove(int count);
	}
}