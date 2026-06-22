using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Avalonia.SourceGenerator.CompositionGenerator
{
    public partial class Generator
    {
        CompilationUnitSyntax Unit()
            => CompilationUnit().WithUsings(List(new[]
                {
                    "System",
                    "System.Text",
                    "System.Collections",
                    "System.Collections.Generic"
                }
                .Concat(_config.Usings
                    .Select(x => x.Name)).Select(u => UsingDirective(IdentifierName(u)))));
        
        void SaveTo(CompilationUnitSyntax unit, params string[] path)
        {
            var text = @"
#nullable enable
#pragma warning disable CS0108, CS0114

" +

                       unit.NormalizeWhitespace().ToFullString();
            _output.AddSource(string.Join("_", path), text);
        }

        static SyntaxToken Semicolon() => Token(SyntaxKind.SemicolonToken);

        static FieldDeclarationSyntax DeclareField(string type, string name, params SyntaxKind[] modifiers) =>
            DeclareField(type, name, null, modifiers);

        static FieldDeclarationSyntax DeclareField(string type, string name, EqualsValueClauseSyntax? initializer,
            params SyntaxKind[] modifiers) =>
            FieldDeclaration(
                    VariableDeclaration(ParseTypeName(type),
                        SingletonSeparatedList(
                            VariableDeclarator(name).WithInitializer(initializer))))
                .WithSemicolonToken(Semicolon())
                .WithModifiers(TokenList(modifiers.Select(x => Token(x))));
    }
}
