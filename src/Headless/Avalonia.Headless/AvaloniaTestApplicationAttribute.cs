using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Headless;

/// <summary>
/// Sets up global avalonia test framework using avalonia application builder passed as a parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AvaloniaTestApplicationAttribute : Attribute
{
    [DynamicallyAccessedMembers(HeadlessUnitTestSession.DynamicallyAccessed)]
    public Type AppBuilderEntryPointType { get; }

    /// <summary>
    /// Creates instance of <see cref="AvaloniaTestApplicationAttribute"/>. 
    /// </summary>
    /// <param name="appBuilderEntryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application.
    /// </param>
    public AvaloniaTestApplicationAttribute(
        [DynamicallyAccessedMembers(HeadlessUnitTestSession.DynamicallyAccessed)]
        Type appBuilderEntryPointType)
    {
        AppBuilderEntryPointType = appBuilderEntryPointType;
    }
}
