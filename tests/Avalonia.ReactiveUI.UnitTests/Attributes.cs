using Xunit;

// Required to avoid InvalidOperationException sometimes thrown
// from Splat.MemoizingMRUCache.cs which is not thread-safe.
// Thrown when trying to access WhenActivated concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]