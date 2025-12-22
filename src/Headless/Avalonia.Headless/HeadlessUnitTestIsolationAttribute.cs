using System;

namespace Avalonia.Headless;

/// <summary>
/// Defines the isolation level for headless unit tests,
/// controlling how <see cref="Avalonia.Application"/> and its
/// associated <see cref="Avalonia.Threading.Dispatcher"/> are managed
/// between test runs.
/// </summary>
public enum AvaloniaTestIsolationLevel
{
    /// <summary>
    /// Reuses a single <see cref="Avalonia.Application"/> and <see cref="Avalonia.Threading.Dispatcher"/>
    /// instance across all tests within the assembly.
    /// </summary>
    /// <remarks>
    /// Tests must not rely on any global or persistent state that could leak between runs.
    /// Headless framework won't dispose any resources after tests when using this mode.
    /// </remarks>
    PerAssembly,

    /// <summary>
    /// Recreates the <see cref="Avalonia.Application"/> and  <see cref="Avalonia.Threading.Dispatcher"/>
    /// for each individual test method.
    /// </summary>
    /// <remarks>
    /// This mode ensures complete test isolation, and should be used for tests that modify global
    /// application state or rely on a clean dispatcher environment.
    /// This is the default isolation level if none is specified.
    /// </remarks>
    PerTest
}

/// <summary>
/// Specifies how headless unit tests should be isolated from each other,
/// defining when the test runtime should recreate the
/// <see cref="Avalonia.Application"/> and <see cref="Avalonia.Threading.Dispatcher"/> instances.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AvaloniaTestIsolationAttribute(AvaloniaTestIsolationLevel isolationLevel) : Attribute
{
    /// <summary>
    /// Gets the isolation level for headless tests.
    /// </summary>
    public AvaloniaTestIsolationLevel IsolationLevel { get; } = isolationLevel;
}
