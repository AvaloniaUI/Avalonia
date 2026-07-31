using System;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Identifies an xunit test that starts on Avalonia Dispatcher
/// such that awaited expressions resume on the test's "main thread".
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer(typeof(AvaloniaFactDiscoverer))]
public sealed class AvaloniaFactAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1)
    : FactAttribute(sourceFilePath, sourceLineNumber);
