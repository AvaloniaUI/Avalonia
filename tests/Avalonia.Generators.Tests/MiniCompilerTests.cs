using System;
using System.ComponentModel;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Avalonia.Generators.Tests.Views;
using XamlX;
using XamlX.Parsers;
using Xunit;

namespace Avalonia.Generators.Tests;

public class MiniCompilerTests
{
    private const string AvaloniaXaml = "<TextBlock xmlns='clr-namespace:Avalonia.Controls;assembly=Avalonia' />";
    private const string MiniClass = "namespace Example { public class Valid { public int Foo() => 21; } }";
    private const string MiniValidXaml = "<Valid xmlns='clr-namespace:Example;assembly=Example' />";

    [Fact]
    public void Should_Resolve_Types_From_Simple_Valid_Xaml_Markup()
    {
        var xaml = XDocumentXamlParser.Parse(MiniValidXaml);
        var compilation = CreateBasicCompilation(MiniClass);
        MiniCompiler.CreateDefault(new RoslynTypeSystem(compilation)).Transform(xaml);

        Assert.NotNull(xaml.Root);
    }

    [Fact]
    public void Should_Resolve_Types_From_Simple_Avalonia_Markup()
    {
        var xaml = XDocumentXamlParser.Parse(AvaloniaXaml);
        var compilation = View.CreateAvaloniaCompilation();
        MiniCompiler.CreateDefault(new RoslynTypeSystem(compilation)).Transform(xaml);

        Assert.NotNull(xaml.Root);
    }

    private static CSharpCompilation CreateBasicCompilation(string source) =>
        CSharpCompilation
            .Create("BasicLib", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ITypeDescriptorContext).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ISupportInitialize).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(TypeConverterAttribute).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));
}
