global using Xunit;
global using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Wayland.Embedding.Tests;

// One headless Avalonia app for the whole assembly. Tests run strictly sequentially: they share the
// process-wide compositor singleton (one background thread) and the single UI dispatcher, and each test
// drives both ends of one Wayland connection by hand, so parallelism would interleave their pumps.
[assembly: AvaloniaTestApplication(typeof(TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
