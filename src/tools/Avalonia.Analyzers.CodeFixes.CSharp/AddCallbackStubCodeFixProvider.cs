using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Analyzers.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Avalonia.Analyzers.CodeFixes.CSharp;

/// <summary>
/// Fixes AVP2006 by inserting a stub implementation of the named callback method after the
/// annotated member. The expected signature is provided by the analyzer through the
/// diagnostic's properties, so no semantic work happens here.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddCallbackStubCodeFixProvider))]
[Shared]
public class AddCallbackStubCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.GeneratedPropertyUnboundCallback);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        if (!diagnostic.Properties.TryGetValue(GeneratedPropertyDescriptors.Properties.MethodName, out var methodName) ||
            !diagnostic.Properties.TryGetValue(GeneratedPropertyDescriptors.Properties.Signature, out var signature) ||
            methodName is null ||
            signature is null)
        {
            // The invalid-identifier variant of AVP2006 carries no signature and has no fix.
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var member = root?.FindToken(diagnostic.Location.SourceSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<MemberDeclarationSyntax>()
            .FirstOrDefault();
        if (member is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Add callback method '{methodName}'",
                cancellationToken => AddStubAsync(context.Document, member, signature, cancellationToken),
                nameof(AddCallbackStubCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> AddStubAsync(
        Document document,
        MemberDeclarationSyntax member,
        string signature,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Match the annotated member's indentation and newline style instead of relying on the
        // formatter, which would rewrite the surrounding trivia with the platform newline.
        var indentation = member.GetLeadingTrivia()
            .LastOrDefault(static trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            .ToString();
        var newLine = member.GetTrailingTrivia()
            .LastOrDefault(static trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            .ToString() is { Length: > 0 } memberNewLine ? memberNewLine : "\n";

        // The leading newline separates the stub from the annotated member with a blank line.
        var stub = SyntaxFactory.ParseMemberDeclaration(
            $"{newLine}{indentation}{signature}{newLine}{indentation}{{{newLine}{indentation}}}{newLine}");
        if (stub is null)
        {
            return document;
        }

        stub = stub.WithAdditionalAnnotations(Simplifier.Annotation);
        var newRoot = root.InsertNodesAfter(member, [stub]);

        return document.WithSyntaxRoot(newRoot);
    }
}
