using System.Threading.Tasks;
using Avalonia.NameGenerator.Compiler;
using Avalonia.NameGenerator.Generator;
using Avalonia.NameGenerator.Tests.InitializeComponent.GeneratedCode;
using Avalonia.NameGenerator.Tests.OnlyProperties.GeneratedCode;
using Avalonia.NameGenerator.Tests.Views;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avalonia.NameGenerator.Tests.InitializeComponent
{
    public class InitializeComponentTests
    {
        [Theory]
        [InlineData(InitializeComponentCode.NamedControl, View.NamedControl)]
        [InlineData(InitializeComponentCode.NamedControls, View.NamedControls)]
        [InlineData(InitializeComponentCode.XNamedControl, View.XNamedControl)]
        [InlineData(InitializeComponentCode.XNamedControls, View.XNamedControls)]
        [InlineData(InitializeComponentCode.NoNamedControls, View.NoNamedControls)]
        [InlineData(InitializeComponentCode.CustomControls, View.CustomControls)]
        [InlineData(InitializeComponentCode.DataTemplates, View.DataTemplates)]
        [InlineData(InitializeComponentCode.SignUpView, View.SignUpView)]
        [InlineData(InitializeComponentCode.AttachedProps, View.AttachedProps)]
        [InlineData(InitializeComponentCode.FieldModifier, View.FieldModifier)]
        public async Task Should_Generate_FindControl_Refs_From_Avalonia_Markup_File(string expectation, string markup)
        {
            var compilation =
                View.CreateAvaloniaCompilation()
                    .WithCustomTextBox();

            var classResolver = new XamlXViewResolver(
                new RoslynTypeSystem(compilation),
                MiniCompiler.CreateDefault(
                    new RoslynTypeSystem(compilation),
                    MiniCompiler.AvaloniaXmlnsDefinitionAttribute));

            var xaml = await View.Load(markup);
            var classInfo = classResolver.ResolveView(xaml);
            var nameResolver = new XamlXNameResolver();
            var names = nameResolver.ResolveNames(classInfo.Xaml);

            var generator = new InitializeComponentCodeGenerator();
            var code = generator
                .GenerateCode("SampleView", "Sample.App", names)
                .Replace("\r", string.Empty);

            var expected = await InitializeComponentCode.Load(expectation);
            CSharpSyntaxTree.ParseText(code);
            Assert.Equal(expected.Replace("\r", string.Empty), code);
        }
    }
}