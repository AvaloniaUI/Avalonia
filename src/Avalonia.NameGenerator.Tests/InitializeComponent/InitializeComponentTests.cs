using System.Threading.Tasks;
using Avalonia.NameGenerator.Compiler;
using Avalonia.NameGenerator.Generator;
using Avalonia.NameGenerator.Tests.InitializeComponent.GeneratedDevTools;
using Avalonia.NameGenerator.Tests.InitializeComponent.GeneratedInitializeComponent;
using Avalonia.NameGenerator.Tests.OnlyProperties.GeneratedCode;
using Avalonia.NameGenerator.Tests.Views;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avalonia.NameGenerator.Tests.InitializeComponent
{
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
        [InlineData(InitializeComponentCode.AttachedProps, View.AttachedProps, false)]
        [InlineData(InitializeComponentCode.FieldModifier, View.FieldModifier, false)]
        [InlineData(DevToolsCode.NamedControl, View.NamedControl, true)]
        [InlineData(DevToolsCode.NamedControls, View.NamedControls, true)]
        [InlineData(DevToolsCode.XNamedControl, View.XNamedControl, true)]
        [InlineData(DevToolsCode.XNamedControls, View.XNamedControls, true)]
        [InlineData(DevToolsCode.NoNamedControls, View.NoNamedControls, true)]
        [InlineData(DevToolsCode.CustomControls, View.CustomControls, true)]
        [InlineData(DevToolsCode.DataTemplates, View.DataTemplates, true)]
        [InlineData(DevToolsCode.SignUpView, View.SignUpView, true)]
        [InlineData(DevToolsCode.AttachedProps, View.AttachedProps, true)]
        [InlineData(DevToolsCode.FieldModifier, View.FieldModifier, true)]
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

            var generator = new InitializeComponentCodeGenerator(types);
            var code = generator
                .GenerateCode("SampleView", "Sample.App", names)
                .Replace("\r", string.Empty);

            var expected = devToolsMode
                ? await DevToolsCode.Load(expectation)
                : await InitializeComponentCode.Load(expectation);

            CSharpSyntaxTree.ParseText(code);
            Assert.Equal(expected.Replace("\r", string.Empty), code);
        }
    }
}