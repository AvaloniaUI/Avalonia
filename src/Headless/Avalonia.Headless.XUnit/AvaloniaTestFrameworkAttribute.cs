using System.Diagnostics.CodeAnalysis;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

/// <summary>
/// Sets up global avalonia test framework using avalonia application builder passed as a parameter.
/// </summary>
[TestFrameworkDiscoverer("Avalonia.Headless.XUnit.AvaloniaTestFrameworkTypeDiscoverer", "Avalonia.Headless.XUnit")]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AvaloniaTestFrameworkAttribute : Attribute, ITestFrameworkAttribute
{
    /// <summary>
    /// Creates instance of <see cref="AvaloniaTestFrameworkAttribute"/>. 
    /// </summary>
    /// <param name="appBuilderEntryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application.
    /// </param>
    public AvaloniaTestFrameworkAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type appBuilderEntryPointType) { }
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
        var builderType = attribute.GetConstructorArguments().First() as Type
            ?? throw new InvalidOperationException("AppBuilderEntryPointType parameter must be defined on the AvaloniaTestFrameworkAttribute attribute.");
        return typeof(AvaloniaTestFramework<>).MakeGenericType(builderType);
    }
}
