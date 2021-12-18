namespace Generic.Models {
	public interface IMutableTree<TElement, TSelf> : ITree<TElement, TSelf>
	where TSelf : IMutableTree<TElement, TSelf> {
		bool TryRemove(TElement element);

		TSelf Prune();
	}
}