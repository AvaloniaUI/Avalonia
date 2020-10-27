using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XamlNameReferenceGenerator.Infrastructure;
using XamlX;
using XamlX.Parsers;
using Xunit;

namespace XamlNameReferenceGenerator.Tests
{
    public class MiniCompilerTests
    {
        private const string MiniClass = "namespace Example { public class Valid { public int Foo() => 21; } }";
        private const string MiniInvalidXaml = "<Invalid xmlns='clr-namespace:Example;assembly=Example' />";
        private const string MiniValidXaml = "<Valid xmlns='clr-namespace:Example;assembly=Example' />";

        [Fact]
        public void Should_Resolve_Types_From_Valid_Xaml_Markup()
        {
            var xaml = XDocumentXamlParser.Parse(MiniValidXaml);
            var compilation = CreateBasicCompilation(MiniClass, "Example");

            MiniCompiler
                .CreateDefault(new RoslynTypeSystem(compilation))
                .Transform(xaml);

            Assert.NotNull(xaml.Root);
        }

        [Fact]
        public void Should_Throw_When_Unable_To_Resolve_Types()
        {
            var xaml = XDocumentXamlParser.Parse(MiniInvalidXaml);
            var compilation = CreateBasicCompilation(MiniClass, "Example");
            var compiler = MiniCompiler.CreateDefault(new RoslynTypeSystem(compilation));

            Assert.Throws<XamlParseException>(() => compiler.Transform(xaml));
        }

        private static CSharpCompilation CreateBasicCompilation(string source, string name) =>
            CSharpCompilation
                .Create(name, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(ITypeDescriptorContext).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(ISupportInitialize).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(TypeConverterAttribute).Assembly.Location))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));
    }
}