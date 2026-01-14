using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
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
    [InlineData(InitializeComponentCode.ControlWithoutWindow, View.ControlWithoutWindow, false)]
    public async Task Should_Generate_FindControl_Refs_From_Avalonia_Markup_File(
        string expectation,
        string markup,
        bool devToolsMode)
    {
        var excluded = devToolsMode ? null : "Avalonia.Diagnostics";

        // Step 1: parse XAML as xml nodes, without any type information.
        var classResolver = new XamlXViewResolver(MiniCompiler.CreateNoop());

        var xaml = await View.Load(markup);
        var classInfo = classResolver.ResolveView(xaml, CancellationToken.None);
        Assert.NotNull(classInfo);
        var nameResolver = new XamlXNameResolver();
        var names = nameResolver.ResolveXmlNames(classInfo.Xaml, CancellationToken.None);

        // Step 2: use compilation context to resolve types
        var compilation =
            View.CreateAvaloniaCompilation(excluded)
                .WithCustomTextBox();
        var resolvedNames = names.ResolveNames(compilation, nameResolver).ToArray();

        // Step 3: run generator
        var generator = new InitializeComponentCodeGenerator(devToolsMode);
        var generatorVersion = typeof(InitializeComponentCodeGenerator).Assembly.GetName().Version?.ToString();

        var code = generator
            .GenerateCode("SampleView", "Sample.App",  resolvedNames)
            .Replace("\r", string.Empty);

        var expected = (await InitializeComponentCode.Load(expectation))
            .Replace("\r", string.Empty)
            .Replace("$GeneratorVersion", generatorVersion);
            
        CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(expected, code);
    }
}
