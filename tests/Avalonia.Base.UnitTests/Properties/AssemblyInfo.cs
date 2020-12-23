using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("Avalonia.UnitTests")]

// Don't run tests in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
