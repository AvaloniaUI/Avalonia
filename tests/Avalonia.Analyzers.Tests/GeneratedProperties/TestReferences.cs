using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avalonia.Analyzers.Tests.GeneratedProperties;

internal static class TestReferences
{
    public static readonly string DefaultTargetFramework = "net" + Environment.Version.ToString(2);

    public static readonly Lazy<IReadOnlyList<MetadataReference>> All = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(static MetadataReference (path) => MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(AvaloniaObject).Assembly.Location))
        .ToList());
}
