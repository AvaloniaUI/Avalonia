using Xunit;

// Tests share one GPU context and the Avalonia locator.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
