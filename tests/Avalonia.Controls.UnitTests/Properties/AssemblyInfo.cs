using System.Reflection;
using Avalonia.UnitTests;
using Xunit;

[assembly: AssemblyTitle("Avalonia.Controls.UnitTests")]

// Don't run tests in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: VerifyEmptyDispatcherAfterTest]
