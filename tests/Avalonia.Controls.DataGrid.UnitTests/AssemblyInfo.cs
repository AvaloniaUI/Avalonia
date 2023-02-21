using Avalonia.UnitTests;
using Xunit;

[assembly: AvaloniaTestFramework(typeof(HeadlessApplication))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = false, MaxParallelThreads = 1)]
