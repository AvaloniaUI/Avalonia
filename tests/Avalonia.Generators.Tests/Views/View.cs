using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Avalonia.Generators.Tests.Views;

public static class View
{
    public const string NamedControl = "NamedControl.xml";
    public const string NamedControls = "NamedControls.xml";
    public const string XNamedControl = "xNamedControl.xml";
    public const string XNamedControls = "xNamedControls.xml";
    public const string NoNamedControls = "NoNamedControls.xml";
    public const string CustomControls = "CustomControls.xml";
    public const string DataTemplates = "DataTemplates.xml";
    public const string SignUpView = "SignUpView.xml";
    public const string AttachedProps = "AttachedProps.xml";
    public const string FieldModifier = "FieldModifier.xml";
    public const string ControlWithoutWindow = "ControlWithoutWindow.xml";
    public const string ViewWithGenericBaseView = "ViewWithGenericBaseView.xml";

    public static async Task<string> Load(string viewName)
    {
        var assembly = typeof(XamlXNameResolverTests).Assembly;
        var fullResourceName = assembly
            .GetManifestResourceNames()
            .First(name => name.EndsWith(viewName));

        await using var stream = assembly.GetManifestResourceStream(fullResourceName);
        using var reader = new StreamReader(stream!);
        return await reader.ReadToEndAsync();
    }

    public static CSharpCompilation CreateAvaloniaCompilation(string excludedPattern = null)
    {
        var compilation = CSharpCompilation
            .Create("AvaloniaLib", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ITypeDescriptorContext).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ISupportInitialize).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(TypeConverterAttribute).Assembly.Location));

        var avaloniaAssemblyLocation = typeof(TextBlock).Assembly.Location;
        var avaloniaAssemblyDirectory = Path.GetDirectoryName(avaloniaAssemblyLocation);
        var avaloniaAssemblyReferences = Directory
            .EnumerateFiles(avaloniaAssemblyDirectory!)
            .Where(file => file.EndsWith(".dll") &&
                           file.Contains("Avalonia") &&
                           (string.IsNullOrWhiteSpace(excludedPattern) || !file.Contains(excludedPattern)))
            .Select(file => MetadataReference.CreateFromFile(file))
            .ToList();

        return compilation.AddReferences(avaloniaAssemblyReferences);
    }

    public static CSharpCompilation WithCustomTextBox(this CSharpCompilation compilation) =>
        compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(
                "using Avalonia.Controls;" +
                "namespace Controls {" +
                "  public class CustomTextBox : TextBox { }" +
                "  public class EvilControl { }" +
                "}"));

    public static CSharpCompilation WithBaseView(this CSharpCompilation compilation) =>
        compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(
                "using Avalonia.Controls;" +
                "namespace Sample.App { public class BaseView<TViewModel> : UserControl { } }"));
}