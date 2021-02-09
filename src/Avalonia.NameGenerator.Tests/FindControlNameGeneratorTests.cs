using System.Threading.Tasks;
using Avalonia.NameGenerator.Compiler;
using Avalonia.NameGenerator.Generator;
using Avalonia.NameGenerator.Tests.GeneratedCode;
using Avalonia.NameGenerator.Tests.Views;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avalonia.NameGenerator.Tests
{
    public class FindControlNameGeneratorTests
    {
        [Theory]
        [InlineData(Code.NamedControl, View.NamedControl)]
        [InlineData(Code.NamedControls, View.NamedControls)]
        [InlineData(Code.XNamedControl, View.XNamedControl)]
        [InlineData(Code.XNamedControls, View.XNamedControls)]
        [InlineData(Code.NoNamedControls, View.NoNamedControls)]
        [InlineData(Code.CustomControls, View.CustomControls)]
        [InlineData(Code.DataTemplates, View.DataTemplates)]
        [InlineData(Code.SignUpView, View.SignUpView)]
        [InlineData(Code.AttachedProps, View.AttachedProps)]
        [InlineData(Code.FieldModifier, View.FieldModifier)]
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

            var generator = new FindControlCodeGenerator();
            var code = generator
                .GenerateCode("SampleView", "Sample.App", names)
                .Replace("\r", string.Empty);

            var expected = await Code.Load(expectation);
            CSharpSyntaxTree.ParseText(code);
            Assert.Equal(expected.Replace("\r", string.Empty), code);
        }
    }
}