using System.Collections.Generic;
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

namespace Avalonia.Analyzers.CodeFixes.CSharp;

/// <summary>
/// Fixes AVP2007 by adding the missing <c>partial</c> modifier to the annotated member and to
/// every non-partial type in its nesting chain. The language-version variant of AVP2007 has no
/// code fix.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakePartialCodeFixProvider))]
[Shared]
public class MakePartialCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.GeneratedPropertyNotPartial);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        if (diagnostic.Properties.TryGetValue(GeneratedPropertyDescriptors.Properties.Defect, out var defect) &&
            defect == GeneratedPropertyDescriptors.Properties.DefectLanguageVersion)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var member = root?.FindToken(diagnostic.Location.SourceSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<MemberDeclarationSyntax>()
            .FirstOrDefault();
        if (member is null || GetNodesMissingPartial(member).Count == 0)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Make member and containing types partial",
                cancellationToken => MakePartialAsync(context.Document, member, cancellationToken),
                nameof(MakePartialCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> MakePartialAsync(
        Document document,
        MemberDeclarationSyntax member,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var newRoot = root.ReplaceNodes(
            GetNodesMissingPartial(member),
            static (_, rewritten) => rewritten.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

        return document.WithSyntaxRoot(newRoot);
    }

    private static List<MemberDeclarationSyntax> GetNodesMissingPartial(MemberDeclarationSyntax member)
    {
        var nodes = new List<MemberDeclarationSyntax>();

        if (!member.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            nodes.Add(member);
        }

        foreach (var type in member.Ancestors().OfType<TypeDeclarationSyntax>())
        {
            if (!type.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                nodes.Add(type);
            }
        }

        return nodes;
    }
}
