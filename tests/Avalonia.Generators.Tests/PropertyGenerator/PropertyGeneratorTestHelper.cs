using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Generators.PropertyGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avalonia.Generators.Tests.PropertyGenerator;

internal static class PropertyGeneratorTestHelper
{
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp14);

    private static readonly Lazy<IReadOnlyList<MetadataReference>> s_references = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(static MetadataReference (path) => MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(AvaloniaObject).Assembly.Location))
        .ToList());

    public static CSharpCompilation CreateCompilation(string source, CSharpParseOptions? parseOptions = null) =>
        CSharpCompilation.Create(
            "PropertyGeneratorTests",
            [CSharpSyntaxTree.ParseText(source, parseOptions ?? ParseOptions)],
            s_references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static (GeneratorDriverRunResult Result, Compilation Output) RunGenerator(
        CSharpCompilation compilation,
        CSharpParseOptions? parseOptions = null)
    {
        var driver = CreateDriver(parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    public static GeneratorDriver CreateDriver(CSharpParseOptions? parseOptions = null) =>
        CSharpGeneratorDriver.Create(
            [new AvaloniaPropertyIncrementalGenerator().AsSourceGenerator()],
            parseOptions: parseOptions ?? ParseOptions,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

    /// <summary>
    /// Runs the generator, asserts it reports no diagnostics and the updated compilation has no
    /// errors, then compares the generated source against GeneratedCode/{sampleName}.txt.
    /// A missing golden file is created from the actual output and the test fails, so new
    /// snapshots are always reviewed before they pass.
    /// </summary>
    public static void AssertGeneratedCode(
        string sampleName,
        [StringSyntax("csharp")] string source,
        string? expectedHintName = null,
        [CallerFilePath] string callerFilePath = "")
    {
        source = """
                 using Avalonia;
                 using Avalonia.Data;

                 """ + source;
        var (result, output) = RunGenerator(CreateCompilation(source));

        Assert.Empty(result.Diagnostics);

        var errors = output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            var generatedText = string.Join(
                "\n\n",
                result.GeneratedTrees.Select(static tree => $"// {tree.FilePath}\n{tree.GetText()}"));
            Assert.Fail(
                $"Generated compilation has errors:\n{string.Join("\n", errors)}\n\nGenerated source:\n{generatedText}");
        }

        var tree = Assert.Single(result.GeneratedTrees);
        if (expectedHintName is not null)
        {
            Assert.EndsWith(expectedHintName, tree.FilePath.Replace('\\', '/'), StringComparison.Ordinal);
        }

        var actual = Normalize(tree.GetText().ToString());
        var expectedPath = Path.Combine(Path.GetDirectoryName(callerFilePath)!, "GeneratedCode", sampleName + ".txt");

        if (!File.Exists(expectedPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.WriteAllText(expectedPath, actual);
            Assert.Fail($"Expected file did not exist and was created from actual output; review it: {expectedPath}");
        }

        var expected = Normalize(File.ReadAllText(expectedPath));
        if (expected != actual)
        {
            File.WriteAllText(expectedPath + ".received", actual);
            Assert.Equal(expected, actual);
        }
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n');
}
