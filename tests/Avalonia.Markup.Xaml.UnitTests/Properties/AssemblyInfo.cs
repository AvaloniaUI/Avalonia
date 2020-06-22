using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("Avalonia.Markup.Xaml.UnitTests")]

// Don't run tests in parallel.
[assembly: CollectionBehavior(MaxParallelThreads = 1)]
