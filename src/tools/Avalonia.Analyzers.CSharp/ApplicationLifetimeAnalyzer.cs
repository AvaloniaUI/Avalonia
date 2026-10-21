using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers;

/// <summary>
/// Reports unchecked casts of Application.ApplicationLifetime and MainWindow reads used as a global window locator.
/// The lifetime is not guaranteed to be set (designer previewer, unit tests, setup without a lifetime)
/// or to be of a specific type (mobile, browser).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApplicationLifetimeAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor s_castRule = new(
        DiagnosticIds.ApplicationLifetimeCast,
        "ApplicationLifetime is cast without a type check",
        "ApplicationLifetime is cast to '{0}' without a type check. The lifetime may be null (designer previewer, unit tests, setup without a lifetime) or of another type (mobile, browser). Use pattern matching and handle the other cases.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_mainWindowAccessRule = new(
        DiagnosticIds.MainWindowAccess,
        "MainWindow is read outside of application initialization",
        "Avoid reading MainWindow outside of application initialization. In multi-window apps it may not be the window the code is running in. Resolve the TopLevel from the context (TopLevel.GetTopLevel).",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_castRule, s_mainWindowAccessRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            var compilation = start.Compilation;
            var application = compilation.GetTypeByMetadataName("Avalonia.Application");
            var lifetime = compilation.GetTypeByMetadataName("Avalonia.Controls.ApplicationLifetimes.IApplicationLifetime");
            var desktopLifetime = compilation.GetTypeByMetadataName("Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime");
            if (application is null || lifetime is null || desktopLifetime is null)
            {
                return;
            }

            var lifetimeProperty = FindProperty(application, "ApplicationLifetime");
            var mainWindowProperty = FindProperty(desktopLifetime, "MainWindow");
            if (lifetimeProperty is null || mainWindowProperty is null)
            {
                return;
            }

            var symbols = new Symbols(application, lifetime, lifetimeProperty, mainWindowProperty);
            start.RegisterOperationAction(context => AnalyzePropertyReference(context, symbols), OperationKind.PropertyReference);
            start.RegisterOperationAction(context => AnalyzeConversion(context, symbols), OperationKind.Conversion);
        });
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, Symbols symbols)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        if (!IsMainWindow(reference.Property, symbols.MainWindowProperty)
            || IsAssignmentTarget(reference)
            || IsInsideNameOf(reference)
            || IsInsideApplication(context.ContainingSymbol, symbols.Application))
        {
            return;
        }

        // Point at the property name rather than the whole "instance.MainWindow" expression.
        var syntax = reference.Syntax switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            var other => other
        };

        context.ReportDiagnostic(Diagnostic.Create(s_mainWindowAccessRule, syntax.GetLocation()));
    }

    private static void AnalyzeConversion(OperationAnalysisContext context, Symbols symbols)
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
            || !SymbolEqualityComparer.Default.Equals(reference.Property, symbols.LifetimeProperty)
            || !IsLifetimeType(targetType, symbols.Lifetime))
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

    private static bool IsMainWindow(IPropertySymbol property, IPropertySymbol mainWindowProperty)
    {
        if (property.Name != mainWindowProperty.Name)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(property, mainWindowProperty)
               || SymbolEqualityComparer.Default.Equals(
                   property.ContainingType.FindImplementationForInterfaceMember(mainWindowProperty),
                   property);
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

    private static bool IsAssignmentTarget(IOperation operation)
        => operation.Parent is ISimpleAssignmentOperation assignment && assignment.Target == operation;

    private static bool IsInsideNameOf(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is INameOfOperation)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideApplication(ISymbol containingSymbol, INamedTypeSymbol application)
    {
        for (var type = containingSymbol.ContainingType; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, application))
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

    private sealed class Symbols(
        INamedTypeSymbol application,
        INamedTypeSymbol lifetime,
        IPropertySymbol lifetimeProperty,
        IPropertySymbol mainWindowProperty)
    {
        public INamedTypeSymbol Application { get; } = application;
        public INamedTypeSymbol Lifetime { get; } = lifetime;
        public IPropertySymbol LifetimeProperty { get; } = lifetimeProperty;
        public IPropertySymbol MainWindowProperty { get; } = mainWindowProperty;
    }
}
