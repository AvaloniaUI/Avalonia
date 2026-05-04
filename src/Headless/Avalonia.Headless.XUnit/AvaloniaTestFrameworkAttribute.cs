using System;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Sets up global avalonia test framework using avalonia application builder passed as a parameter.
/// </summary>
/// <remarks>
/// It is an alternative to using [AvaloniaFact] or [AvaloniaTheory] attributes on every test method.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AvaloniaTestFrameworkAttribute : Attribute, ITestFrameworkAttribute
{
    public Type FrameworkType
        => typeof(AvaloniaTestFramework);
}
