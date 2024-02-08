using System.Threading.Tasks;
using Avalonia.Generators.Common;
using Avalonia.Generators.Compiler;
using Avalonia.Generators.NameGenerator;
using Avalonia.Generators.Tests.InitializeComponent.GeneratedInitializeComponent;
using Avalonia.Generators.Tests.Views;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avalonia.Generators.Tests.InitializeComponent;

public class InitializeComponentTests
{
    [Theory]
    [InlineData(InitializeComponentCode.NamedControl, View.NamedControl, false)]
    [InlineData(InitializeComponentCode.NamedControls, View.NamedControls, false)]
    [InlineData(InitializeComponentCode.XNamedControl, View.XNamedControl, false)]
    [InlineData(InitializeComponentCode.XNamedControls, View.XNamedControls, false)]
    [InlineData(InitializeComponentCode.NoNamedControls, View.NoNamedControls, false)]
    [InlineData(InitializeComponentCode.CustomControls, View.CustomControls, false)]
    [InlineData(InitializeComponentCode.DataTemplates, View.DataTemplates, false)]
    [InlineData(InitializeComponentCode.SignUpView, View.SignUpView, false)]
    [InlineData(InitializeComponentCode.FieldModifier, View.FieldModifier, false)]
    [InlineData(InitializeComponentCode.AttachedPropsWithDevTools, View.AttachedProps, true)]
    [InlineData(InitializeComponentCode.AttachedProps, View.AttachedProps, false)]
    [InlineData(InitializeComponentCode.ControlWithoutWindow, View.ControlWithoutWindow, true)]
    [InlineData(InitializeComponentCode.ControlWithoutWindow, View.ControlWithoutWindow, false)]
    public async Task Should_Generate_FindControl_Refs_From_Avalonia_Markup_File(
        string expectation,
        string markup,
        bool devToolsMode)
    {
        var excluded = devToolsMode ? null : "Avalonia.Diagnostics";
        var compilation =
            View.CreateAvaloniaCompilation(excluded)
                .WithCustomTextBox();

        var types = new RoslynTypeSystem(compilation);
        var classResolver = new XamlXViewResolver(
            types,
            MiniCompiler.CreateDefault(
                new RoslynTypeSystem(compilation),
                MiniCompiler.AvaloniaXmlnsDefinitionAttribute));

        var xaml = await View.Load(markup);
        var classInfo = classResolver.ResolveView(xaml);
        var nameResolver = new XamlXNameResolver();
        var names = nameResolver.ResolveNames(classInfo.Xaml);

        var generator = new InitializeComponentCodeGenerator(types, devToolsMode);

        var code = generator
            .GenerateCode("SampleView", "Sample.App",  classInfo.XamlType, names)
            .Replace("\r", string.Empty);

        var expected = await InitializeComponentCode.Load(expectation);
            
            
        CSharpSyntaxTree.ParseText(code);
        Assert.Equal(expected.Replace("\r", string.Empty), code);
    }
}
