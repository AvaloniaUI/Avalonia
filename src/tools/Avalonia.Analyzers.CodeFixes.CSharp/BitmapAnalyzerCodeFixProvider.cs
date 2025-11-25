using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Avalonia.Analyzers.CodeFixes.CSharp;

/// <summary>
/// Provides a code fix for the BitmapAnalyzer diagnostic, which replaces "avares://" string arguments
/// with a call to AssetLoader.Open(new Uri("avares://...")).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BitmapAnalyzerCodeFixProvider))]
[Shared]
public class BitmapAnalyzerCodeFixProvider : CodeFixProvider
{
    private const string _title = "Use AssetLoader to open assets as stream first";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.Bitmap);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
    
    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                              .OfType<LocalDeclarationStatementSyntax>().First();

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                _title,
                c => ReplaceArgumentAsync(context.Document, declaration, c),
                _title),
            diagnostic);
    }

    private async Task<Document> ReplaceArgumentAsync(Document contextDocument, LocalDeclarationStatementSyntax declaration,
        CancellationToken cancellationToken)
    {
        var root = await contextDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await contextDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var objectCreation = declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
        var argumentList = objectCreation.ArgumentList;
        var newArguments = argumentList.Arguments.Select(arg =>
        {
            var constantValue = semanticModel.GetConstantValue(arg.Expression);
            if (constantValue.HasValue && constantValue.Value is string stringValue &&
                stringValue.StartsWith("avares://"))
            {
                var newArgument = SyntaxFactory.Argument(
                    SyntaxFactory.InvocationExpression(
                                     SyntaxFactory.MemberAccessExpression(
                                         SyntaxKind.SimpleMemberAccessExpression,
                                         SyntaxFactory.IdentifierName("AssetLoader"),
                                         SyntaxFactory.IdentifierName("Open")))
                                 .WithArgumentList(
                                     SyntaxFactory.ArgumentList(
                                         SyntaxFactory.SingletonSeparatedList(
                                             SyntaxFactory.Argument(
                                                 SyntaxFactory.ObjectCreationExpression(
                                                                  SyntaxFactory.IdentifierName("Uri"))
                                                              .WithArgumentList(
                                                                  SyntaxFactory.ArgumentList(
                                                                      SyntaxFactory.SingletonSeparatedList(
                                                                          SyntaxFactory.Argument(
                                                                              SyntaxFactory.LiteralExpression(
                                                                                  SyntaxKind.StringLiteralExpression,
                                                                                  SyntaxFactory
                                                                                      .Literal(stringValue)))))))))));
                return newArgument;
            }

            return arg;
        }).ToArray();

        var newArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArguments));
        var newObjectCreation = objectCreation.WithArgumentList(newArgumentList);
        var newRoot = root.ReplaceNode(objectCreation, newObjectCreation);

        var usingDirective = ((CompilationUnitSyntax)newRoot).Usings;
        var newUsings = new List<UsingDirectiveSyntax>();
        if(!usingDirective.Any(a=>a.Name.ToString().Contains("Avalonia.Platform")))
        {
            newUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Avalonia.Platform")));
        }
        if(!usingDirective.Any(a=>a.Name.ToString().Contains("System")))
        {
            newUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
        }
        // Add the new using directives to the root
        newRoot = ((CompilationUnitSyntax)newRoot).AddUsings(newUsings.ToArray());

        return contextDocument.WithSyntaxRoot(newRoot);
    }
}
