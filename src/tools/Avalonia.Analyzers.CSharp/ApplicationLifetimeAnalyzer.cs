using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers;

/// <summary>
/// Reports unchecked casts of Application.ApplicationLifetime.
/// The lifetime is not guaranteed to be set (designer previewer, unit tests, setup without a lifetime)
/// or to be of a specific type (mobile, browser).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApplicationLifetimeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_castRule = new(
        DiagnosticIds.ApplicationLifetimeCast,
        "ApplicationLifetime is cast without a type check",
        "ApplicationLifetime is cast to '{0}' without a type check. The lifetime may be null (designer previewer, unit tests, setup without a lifetime) or of another type (mobile, browser). Use pattern matching and handle the other cases.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_castRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            var compilation = start.Compilation;
            var application = compilation.GetTypeByMetadataName("Avalonia.Application");
            var lifetime = compilation.GetTypeByMetadataName("Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime");
            if (application is null || lifetime is null)
            {
                return;
            }

            var lifetimeProperty = FindProperty(application, "ApplicationLifetime");
            if (lifetimeProperty is null)
            {
                return;
            }

            start.RegisterOperationAction(
                context => AnalyzeConversion(context, lifetime, lifetimeProperty),
                OperationKind.Conversion);
        });
    }

    private static void AnalyzeConversion(
        OperationAnalysisContext context,
        INamedTypeSymbol lifetime,
        IPropertySymbol lifetimeProperty)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (conversion.Conversion.IsImplicit || conversion.Type is not { } targetType)
        {
            return;
        }

        if (conversion.IsTryCast && !IsNullSuppressed(conversion.Syntax))
        {
            return;
        }

        var operand = conversion.Operand;
        if (operand is IConditionalAccessOperation conditionalAccess)
        {
            operand = conditionalAccess.WhenNotNull;
        }

        if (operand is not IPropertyReferenceOperation reference
            || !SymbolEqualityComparer.Default.Equals(reference.Property, lifetimeProperty)
            || !IsLifetimeType(targetType, lifetime))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            s_castRule,
            conversion.Syntax.GetLocation(),
            targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static IPropertySymbol? FindProperty(INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
        {
            if (member is IPropertySymbol property)
            {
                return property;
            }
        }

        return null;
    }

    private static bool IsLifetimeType(ITypeSymbol type, INamedTypeSymbol lifetime)
    {
        if (SymbolEqualityComparer.Default.Equals(type, lifetime))
        {
            return true;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(@interface, lifetime))
            {
                return true;
            }
        }

        return false;
    }

    // "(x as T)!" is a hard cast in disguise.
    private static bool IsNullSuppressed(SyntaxNode syntax)
    {
        var parent = syntax.Parent;
        while (parent is ParenthesizedExpressionSyntax)
        {
            parent = parent.Parent;
        }

        return parent.IsKind(SyntaxKind.SuppressNullableWarningExpression);
    }
}
