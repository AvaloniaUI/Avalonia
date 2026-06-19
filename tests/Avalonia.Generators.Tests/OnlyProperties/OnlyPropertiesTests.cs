using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Avalonia.Generators.NameGenerator;
using Avalonia.Generators.Tests.OnlyProperties.GeneratedCode;
using Avalonia.Generators.Tests.Views;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avalonia.Generators.Tests.OnlyProperties;

public class OnlyPropertiesTests
{
    [Theory]
    [InlineData(OnlyPropertiesCode.NamedControl, View.NamedControl)]
    [InlineData(OnlyPropertiesCode.NamedControls, View.NamedControls)]
    [InlineData(OnlyPropertiesCode.XNamedControl, View.XNamedControl)]
    [InlineData(OnlyPropertiesCode.XNamedControls, View.XNamedControls)]
    [InlineData(OnlyPropertiesCode.NoNamedControls, View.NoNamedControls)]
    [InlineData(OnlyPropertiesCode.CustomControls, View.CustomControls)]
    [InlineData(OnlyPropertiesCode.DataTemplates, View.DataTemplates)]
    [InlineData(OnlyPropertiesCode.SignUpView, View.SignUpView)]
    [InlineData(OnlyPropertiesCode.AttachedProps, View.AttachedProps)]
    [InlineData(OnlyPropertiesCode.FieldModifier, View.FieldModifier)]
    [InlineData(OnlyPropertiesCode.ControlWithoutWindow, View.ControlWithoutWindow)]
    public async Task Should_Generate_FindControl_Refs_From_Avalonia_Markup_File(string expectation, string markup)
    {
        // Step 1: parse XAML as xml nodes, without any type information.
        var classResolver = new XamlXViewResolver(MiniCompiler.CreateNoop());

        var xaml = await View.Load(markup);
        var classInfo = classResolver.ResolveView(xaml, CancellationToken.None);
        Assert.NotNull(classInfo);
        var nameResolver = new XamlXNameResolver();
        var names = nameResolver.ResolveXmlNames(classInfo.Xaml, CancellationToken.None);

        // Step 2: use compilation context to resolve types
        var compilation =
            View.CreateAvaloniaCompilation()
                .WithCustomTextBox();
        var resolvedNames = names.ResolveNames(compilation, nameResolver).ToArray();

        // Step 3: run generator
        var generator = new OnlyPropertiesCodeGenerator();
        var generatorVersion = typeof(OnlyPropertiesCodeGenerator).Assembly.GetName().Version?.ToString();

        var code = generator
            .GenerateCode("SampleView", "Sample.App", resolvedNames)
            .Replace("\r", string.Empty);

        var expected = (await OnlyPropertiesCode.Load(expectation))
            .Replace("\r", string.Empty)
            .Replace("$GeneratorVersion", generatorVersion);

        CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(expected, code);
    }
}
