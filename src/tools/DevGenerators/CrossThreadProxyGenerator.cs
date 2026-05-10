using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Generator;
using Microsoft.CodeAnalysis;

namespace DevGenerators;

/// <summary>
/// Generates cross-thread proxy classes for interfaces marked with
/// <c>[GenerateCrossThreadProxy]</c>. The generated proxy mirrors the source
/// interface but routes every call through a user-supplied marshaller
/// (e.g. a dispatcher post). Void methods become fire-and-forget; non-void
/// methods (and methods explicitly tagged with
/// <c>[GenerateCrossThreadProxyReturnTask]</c>) are wrapped into
/// <see cref="System.Threading.Tasks.Task"/> / <c>Task&lt;T&gt;</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CrossThreadProxyGenerator : IIncrementalGenerator
{
    private const string ProxyAttributeName = "Avalonia.SourceGenerator.GenerateCrossThreadProxyAttribute";
    private const string ReturnTaskAttributeName = "global::Avalonia.SourceGenerator.GenerateCrossThreadProxyReturnTaskAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var proxies = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ProxyAttributeName,
                static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax,
                static (ctx, _) => Extract(ctx))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!);

        context.RegisterSourceOutput(proxies, static (spc, model) =>
        {
            var source = Render(model);
            spc.AddSource(model.HintName, source);
        });
    }

    private static ProxyModel? Extract(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol iface || iface.TypeKind != TypeKind.Interface)
            return null;

        var attr = ctx.Attributes.FirstOrDefault();
        if (attr is null || attr.ConstructorArguments.Length < 2)
            return null;

        var priorityTypeArg = attr.ConstructorArguments[0];
        var defaultExprArg = attr.ConstructorArguments[1];
        if (priorityTypeArg.Value is not INamedTypeSymbol priorityType
            || defaultExprArg.Value is not string defaultExpr)
            return null;

        string? generatedClassName = null;
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "GeneratedClassName" && named.Value.Value is string s)
                generatedClassName = s;
        }

        var className = generatedClassName ?? DefaultProxyName(iface.Name);
        var ns = iface.ContainingNamespace.IsGlobalNamespace
            ? null
            : iface.ContainingNamespace.ToDisplayString();
        var ifaceFqn = iface.GetFullyQualifiedName();
        var priorityFqn = priorityType.GetFullyQualifiedName();

        // Detect a single direct base interface that also carries the proxy attribute;
        // we will inherit from its generated proxy class instead of re-emitting members.
        string? baseProxyFqn = null;
        var inheritedMethodSignatures = new HashSet<string>();
        foreach (var baseIface in iface.Interfaces)
        {
            var baseAttr = baseIface.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == ProxyAttributeName);
            if (baseAttr is null)
                continue;
            if (baseProxyFqn is not null)
                return null; // multiple proxied bases unsupported

            string? baseGenerated = null;
            foreach (var na in baseAttr.NamedArguments)
            {
                if (na.Key == "GeneratedClassName" && na.Value.Value is string s)
                    baseGenerated = s;
            }
            var baseClassName = baseGenerated ?? DefaultProxyName(baseIface.Name);
            var baseNs = baseIface.ContainingNamespace.IsGlobalNamespace
                ? null
                : baseIface.ContainingNamespace.ToDisplayString();
            baseProxyFqn = "global::" + (baseNs is null ? baseClassName : baseNs + "." + baseClassName);

            foreach (var bm in baseIface.AllInterfaces.Concat(new[] { baseIface })
                         .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>()))
            {
                if (bm.MethodKind != MethodKind.Ordinary) continue;
                inheritedMethodSignatures.Add(MethodSignature(bm));
            }
        }

        var methodsBuilder = ImmutableArray.CreateBuilder<MethodModel>();
        foreach (var member in iface.GetMembers())
        {
            if (member is not IMethodSymbol m || m.MethodKind != MethodKind.Ordinary)
                continue;
            if (m.IsGenericMethod)
                continue;
            if (inheritedMethodSignatures.Contains(MethodSignature(m)))
                continue;
            var hasReturnTaskAttr = m.HasAttributeWithFullyQualifiedName(ReturnTaskAttributeName);
            var returnsVoid = m.ReturnsVoid;
            var wantTask = hasReturnTaskAttr || !returnsVoid;

            var paramsBuilder = ImmutableArray.CreateBuilder<ParamModel>();
            var skip = false;
            foreach (var p in m.Parameters)
            {
                if (p.RefKind != RefKind.None)
                {
                    skip = true;
                    break;
                }
                paramsBuilder.Add(new ParamModel(p.Name, p.Type.GetFullyQualifiedName()));
            }
            if (skip)
                continue;

            methodsBuilder.Add(new MethodModel(
                Name: m.Name,
                ReturnTypeFqn: returnsVoid ? null : m.ReturnType.GetFullyQualifiedName(),
                WrapInTask: wantTask,
                Parameters: paramsBuilder.ToImmutable()));
        }

        var hintName = (ns is null ? className : ns + "." + className) + ".g.cs";

        return new ProxyModel(
            HintName: hintName,
            Namespace: ns,
            ClassName: className,
            InterfaceFqn: ifaceFqn,
            PriorityFqn: priorityFqn,
            DefaultPriorityExpression: defaultExpr,
            BaseProxyFqn: baseProxyFqn,
            Methods: methodsBuilder.ToImmutable());
    }

    private static string MethodSignature(IMethodSymbol m)
    {
        var sb = new StringBuilder();
        sb.Append(m.Name).Append('(');
        for (var i = 0; i < m.Parameters.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(m.Parameters[i].Type.GetFullyQualifiedName());
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static string DefaultProxyName(string ifaceName)
    {
        if (ifaceName.Length > 1 && ifaceName[0] == 'I' && char.IsUpper(ifaceName[1]))
            return ifaceName.Substring(1) + "Proxy";
        return ifaceName + "Proxy";
    }

    private static string Render(ProxyModel m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        if (m.Namespace is not null)
        {
            sb.Append("namespace ").Append(m.Namespace).AppendLine(";");
        }

        sb.Append("internal class ").Append(m.ClassName);
        var canImplementInterface = m.Methods.All(mm => !mm.WrapInTask);
        if (m.BaseProxyFqn is not null)
        {
            sb.Append(" : ").Append(m.BaseProxyFqn);
            if (canImplementInterface)
                sb.Append(", ").Append(m.InterfaceFqn);
        }
        else if (canImplementInterface)
        {
            sb.Append(" : ").Append(m.InterfaceFqn);
        }
        sb.AppendLine();
        sb.AppendLine("{");
        sb.Pad(1).Append("private readonly ").Append(m.InterfaceFqn).AppendLine(" _target;");
        sb.Pad(1).Append("private readonly global::System.Action<global::System.Action, ")
            .Append(m.PriorityFqn).AppendLine("> _marshaller;");
        sb.AppendLine();
        sb.Pad(1).Append("public ").Append(m.ClassName).Append("(")
            .Append(m.InterfaceFqn).Append(" target, global::System.Action<global::System.Action, ")
            .Append(m.PriorityFqn).Append("> marshaller)");
        if (m.BaseProxyFqn is not null)
            sb.Append(" : base(target, marshaller)");
        sb.AppendLine();
        sb.Pad(1).AppendLine("{");
        sb.Pad(2).AppendLine("_target = target;");
        sb.Pad(2).AppendLine("_marshaller = marshaller;");
        sb.Pad(1).AppendLine("}");

        // Worker-side accessor for identity-passing scenarios (e.g. IWXdgTopLevel.SetParent
        // takes another IWXdgTopLevel which on the worker side must be unwrapped to the real
        // target so we can dereference its protocol object). Hide it from base classes via
        // 'new' to avoid ambiguity in inheritance chains.
        sb.AppendLine();
        sb.Pad(1).Append("internal ");
        if (m.BaseProxyFqn is not null)
            sb.Append("new ");
        sb.Append(m.InterfaceFqn).AppendLine(" ProxyTarget => _target;");

        foreach (var method in m.Methods)
        {
            sb.AppendLine();
            RenderMethod(sb, m, method);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void RenderMethod(StringBuilder sb, ProxyModel m, MethodModel method)
    {
        var paramListNoPriority = string.Join(", ",
            method.Parameters.Select(p => p.TypeFqn + " " + p.Name));
        var paramListWithPriority = paramListNoPriority.Length > 0
            ? paramListNoPriority + ", " + m.PriorityFqn + " priority"
            : m.PriorityFqn + " priority";

        var argList = string.Join(", ", method.Parameters.Select(p => p.Name));
        var argListPlusDefaultPriority = argList.Length > 0
            ? argList + ", " + m.DefaultPriorityExpression
            : m.DefaultPriorityExpression;

        string returnType;
        if (!method.WrapInTask)
            returnType = "void";
        else if (method.ReturnTypeFqn is null)
            returnType = "global::System.Threading.Tasks.Task";
        else
            returnType = "global::System.Threading.Tasks.Task<" + method.ReturnTypeFqn + ">";

        // Convenience overload — uses the configured default priority.
        sb.Pad(1).Append("public ").Append(returnType).Append(' ').Append(method.Name)
            .Append('(').Append(paramListNoPriority).Append(") => ")
            .Append(method.Name).Append('(').Append(argListPlusDefaultPriority).AppendLine(");");
        sb.AppendLine();

        sb.Pad(1).Append("public ").Append(returnType).Append(' ').Append(method.Name)
            .Append('(').Append(paramListWithPriority).AppendLine(")");
        sb.Pad(1).AppendLine("{");

        if (!method.WrapInTask)
        {
            // Fire-and-forget void.
            sb.Pad(2).Append("_marshaller(() => _target.").Append(method.Name)
                .Append('(').Append(argList).AppendLine("), priority);");
        }
        else
        {
            // Task / Task<T> wrapper.
            string tcsType;
            if (method.ReturnTypeFqn is null)
                tcsType = "global::System.Threading.Tasks.TaskCompletionSource<bool>";
            else
                tcsType = "global::System.Threading.Tasks.TaskCompletionSource<" + method.ReturnTypeFqn + ">";

            sb.Pad(2).Append("var tcs = new ").Append(tcsType)
                .AppendLine("(global::System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);");
            sb.Pad(2).AppendLine("_marshaller(() =>");
            sb.Pad(2).AppendLine("{");
            sb.Pad(3).AppendLine("try");
            sb.Pad(3).AppendLine("{");
            if (method.ReturnTypeFqn is null)
            {
                sb.Pad(4).Append("_target.").Append(method.Name).Append('(').Append(argList).AppendLine(");");
                sb.Pad(4).AppendLine("tcs.TrySetResult(true);");
            }
            else
            {
                sb.Pad(4).Append("var __result = _target.").Append(method.Name)
                    .Append('(').Append(argList).AppendLine(");");
                sb.Pad(4).AppendLine("tcs.TrySetResult(__result);");
            }
            sb.Pad(3).AppendLine("}");
            sb.Pad(3).AppendLine("catch (global::System.Exception __ex)");
            sb.Pad(3).AppendLine("{");
            sb.Pad(4).AppendLine("tcs.TrySetException(__ex);");
            sb.Pad(3).AppendLine("}");
            sb.Pad(2).AppendLine("}, priority);");
            sb.Pad(2).AppendLine("return tcs.Task;");
        }

        sb.Pad(1).AppendLine("}");
    }

    // ---- equatable models ----

    private sealed record ProxyModel(
        string HintName,
        string? Namespace,
        string ClassName,
        string InterfaceFqn,
        string PriorityFqn,
        string DefaultPriorityExpression,
        string? BaseProxyFqn,
        ImmutableArray<MethodModel> Methods)
    {
        public bool Equals(ProxyModel? other)
        {
            if (other is null) return false;
            return HintName == other.HintName
                && Namespace == other.Namespace
                && ClassName == other.ClassName
                && InterfaceFqn == other.InterfaceFqn
                && PriorityFqn == other.PriorityFqn
                && DefaultPriorityExpression == other.DefaultPriorityExpression
                && BaseProxyFqn == other.BaseProxyFqn
                && Methods.SequenceEqual(other.Methods);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var h = 17;
                h = h * 31 + (HintName?.GetHashCode() ?? 0);
                h = h * 31 + (Namespace?.GetHashCode() ?? 0);
                h = h * 31 + (ClassName?.GetHashCode() ?? 0);
                h = h * 31 + (InterfaceFqn?.GetHashCode() ?? 0);
                h = h * 31 + (PriorityFqn?.GetHashCode() ?? 0);
                h = h * 31 + (DefaultPriorityExpression?.GetHashCode() ?? 0);
                h = h * 31 + (BaseProxyFqn?.GetHashCode() ?? 0);
                foreach (var m in Methods) h = h * 31 + m.GetHashCode();
                return h;
            }
        }
    }

    private sealed record MethodModel(
        string Name,
        string? ReturnTypeFqn,
        bool WrapInTask,
        ImmutableArray<ParamModel> Parameters)
    {
        public bool Equals(MethodModel? other)
        {
            if (other is null) return false;
            return Name == other.Name
                && ReturnTypeFqn == other.ReturnTypeFqn
                && WrapInTask == other.WrapInTask
                && Parameters.SequenceEqual(other.Parameters);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var h = 17;
                h = h * 31 + (Name?.GetHashCode() ?? 0);
                h = h * 31 + (ReturnTypeFqn?.GetHashCode() ?? 0);
                h = h * 31 + WrapInTask.GetHashCode();
                foreach (var p in Parameters) h = h * 31 + p.GetHashCode();
                return h;
            }
        }
    }

    private sealed record ParamModel(string Name, string TypeFqn);
}
