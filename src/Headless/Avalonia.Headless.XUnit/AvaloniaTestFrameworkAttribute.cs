using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Sets up global avalonia test framework using avalonia application builder passed as a parameter.
/// </summary>
/// <remarks>
/// It is an alternative to using [AvaloniaFact] or [AvaloniaTheory] attributes on every test method.
/// </remarks>
[TestFrameworkDiscoverer("Avalonia.Headless.XUnit.AvaloniaTestFrameworkTypeDiscoverer", "Avalonia.Headless.XUnit")]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AvaloniaTestFrameworkAttribute : Attribute, ITestFrameworkAttribute
{
}

/// <summary>
/// Discoverer implementation for the Avalonia testing framework.
/// </summary>
public class AvaloniaTestFrameworkTypeDiscoverer : ITestFrameworkTypeDiscoverer
{
    /// <summary>
    /// Creates instance of <see cref="AvaloniaTestFrameworkTypeDiscoverer"/>. 
    /// </summary>
    public AvaloniaTestFrameworkTypeDiscoverer(IMessageSink _)
    {
    }

    /// <inheritdoc/>
    public Type GetTestFrameworkType(IAttributeInfo attribute)
    {
        return typeof(AvaloniaTestFramework);
    }
}
