using System;
using Xunit;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Identifies an xunit theory that starts on Avalonia Dispatcher
/// such that awaited expressions resume on the test's "main thread".
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer(typeof(AvaloniaTheoryDiscoverer))]
public sealed class AvaloniaTheoryAttribute : TheoryAttribute;
