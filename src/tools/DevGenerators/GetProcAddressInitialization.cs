using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

[Generator(LanguageNames.CSharp)]
public class GetProcAddressInitializationGenerator : IIncrementalGenerator
{
    const string GetProcAddressFullName = "global::Avalonia.SourceGenerator.GetProcAddressAttribute";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var allMethodsWithAttributes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => s is MethodDeclarationSyntax
                {
                    AttributeLists.Count: > 0,
                } md && md.Modifiers.Any(m=>m.IsKind(SyntaxKind.PartialKeyword)),
                static (context, _) =>
                    (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!);

        var fieldsWithAttribute = allMethodsWithAttributes
            .Where(s => s.HasAttributeWithFullyQualifiedName(GetProcAddressFullName));

        var all = fieldsWithAttribute.Collect();
        context.RegisterSourceOutput(all, static (context, methods) =>
        {
            foreach (var typeGroup in methods.GroupBy<IMethodSymbol,ISymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                var nextContext = 0;
                var contexts = new Dictionary<string, int>();

                string GetContextNameFromIndex(int c) => "context" + (c == 0 ? "" : c);
                string GetContextName(string type)
                {
                    if (contexts.TryGetValue(type, out var idx))
                        return GetContextNameFromIndex(idx);
                    if (nextContext != 0)
                        idx += nextContext;
                    nextContext++;
                    return GetContextNameFromIndex(contexts[type] = idx);
                }
                
                var classBuilder = new StringBuilder();
                if (typeGroup.Key.ContainingNamespace != null)
                    classBuilder
                        .AppendLine("using System;")
                        .Append("namespace ")
                        .Append(typeGroup.Key.ContainingNamespace)
                        .AppendLine(";");
                classBuilder
                    .Append("unsafe partial class ")
                    .AppendLine(typeGroup.Key.Name)
                    .AppendLine("{");
                var initializeBody = new StringBuilder()
                    .Pad(2)
                    .AppendLine("var addr = IntPtr.Zero;");
                
                foreach (var method in typeGroup)
                {
                    var isOptional = false;
                    var first = true;
                    var fieldName = "_addr_" + method.Name;
                    var delegateType = BuildDelegateType(classBuilder, method, fieldName);
                    var visibility = method.DeclaredAccessibility == Accessibility.Public
                        ? "public "
                        : method.DeclaredAccessibility == Accessibility.Internal
                            ? "internal "
                            : "";

                    void AppendNextAddr()
                    {
                        if (first)
                        {
                            first = false;
                            initializeBody.Pad(2);
                        }
                        else
                            initializeBody
                                .Pad(2)
                                .Append("if(addr == IntPtr.Zero) ");
                    }

                    initializeBody
                        .Pad(2).Append("// Initializing ").AppendLine(method.Name)
                        .Pad(2)
                        .AppendLine("addr = IntPtr.Zero;");
                    foreach (var attr in method.GetAttributes())
                    {
                        if (attr.AttributeClass?.HasFullyQualifiedName(GetProcAddressFullName) == true)
                        {
                            string? primaryName = null;
                            foreach (var arg in attr.ConstructorArguments)
                            {
                                if (arg.Value is string name)
                                    primaryName = name;
                                if (arg.Value is bool opt)
                                    isOptional = opt;
                            }

                            if (primaryName != null)
                            {
                                AppendNextAddr();
                                initializeBody
                                    .Append("addr = getProcAddress(\"")
                                    .Append(primaryName)
                                    .AppendLine("\");");
                            }
                        }
                        else
                        {
                            if (attr.AttributeClass != null
                                && attr.AttributeClass.MemberNames.Contains("GetProcAddress"))
                            {
                                var getProcMethod = attr.AttributeClass.GetMembers()
                                    .FirstOrDefault(m => m.Name == "GetProcAddress") as IMethodSymbol;
                                if (getProcMethod == null || !getProcMethod.IsStatic || getProcMethod.Parameters.Length < 2)
                                    continue;
                                var contextName =
                                    GetContextName(getProcMethod
                                        .Parameters[1].Type
                                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                                AppendNextAddr();
                                initializeBody
                                    .Append("addr = ")
                                    .Append(attr.AttributeClass.GetFullyQualifiedName())
                                    .Append(".GetProcAddress(")
                                    .Append("getProcAddress, ")
                                    .Append(contextName);

                                if (attr.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntaxNode 
                                    && syntaxNode.ArgumentList is { })
                                {
                                    foreach (var arg in syntaxNode.ArgumentList.Arguments)
                                        initializeBody.Append(", ").Append(arg.GetText());
                                }
                                initializeBody.AppendLine(");");
                            }
                        }
                    }

                    if (!isOptional)
                    {
                        initializeBody
                            .Pad(2)
                            .Append("if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException(\"")
                            .Append(fieldName).AppendLine("\");");
                    }

                    initializeBody
                        .Pad(2)
                        .Append(fieldName)
                        .Append(" = (")
                        .Append(delegateType)
                        .AppendLine(")addr;");

                    classBuilder
                        .Pad(1)
                        .Append(visibility)
                        .Append(" partial ")
                        .Append(method.ReturnType.GetFullyQualifiedName())
                        .Append(" ")
                        .Append(method.Name)
                        .Append("(");
                    var firstArg = true;
                    foreach (var p in method.Parameters)
                    {
                        if (firstArg)
                            firstArg = false;
                        else
                            classBuilder.Append(", ");
                        AppendRefKind(classBuilder, p.RefKind);
                        classBuilder
                            .Append(p.Type.GetFullyQualifiedName())
                            .Append(" @")
                            .Append(p.Name);
                    }
                    classBuilder
                        .AppendLine(")")
                        .Pad(1)
                        .AppendLine("{");
                    if (isOptional)
                        classBuilder
                            .Pad(2)
                            .Append("if (")
                            .Append(fieldName)
                            .Append(" == null) throw new System.EntryPointNotFoundException(\"")
                            .Append(method.Name)
                            .AppendLine("\");");

                    foreach(var p in method.Parameters)
                        if (NeedsPin(p.Type, p.RefKind))
                            classBuilder.Pad(2)
                                .Append("fixed(")
                                .Append(MapToNative(p.Type, p.RefKind))
                                .Append(" @__p_")
                                .Append(p.Name)
                                .Append(" = ")
                                .Append(p.RefKind != RefKind.None ? "&" : "")
                                .Append(p.Name)
                                .AppendLine(")");
                    
                    classBuilder.Pad(2);

                    if (!method.ReturnsVoid)
                        classBuilder.Append("return ");

                    var invokeBuilder = new StringBuilder();
                    
                    invokeBuilder
                        .Append(fieldName)
                        .Append("(");
                    firstArg = true;
                    foreach (var p in method.Parameters)
                    {
                        if (firstArg)
                            firstArg = false;
                        else
                            invokeBuilder.Append(", ");
                        invokeBuilder
                            .Append("@")
                            .Append(ConvertToNative(p.Name, p.Type, p.RefKind));
                    }

                    invokeBuilder.Append(")");
                    classBuilder.Append(ConvertToManaged(method.ReturnType, invokeBuilder.ToString()));
                    
                    classBuilder.AppendLine(";").Pad(1).AppendLine("}");
                    if (isOptional)
                        classBuilder
                            .Pad(1)
                            .Append(visibility)
                            .Append(" bool Is")
                            .Append(method.Name)
                            .Append("Available => ")
                            .Append(fieldName)
                            .AppendLine(" != null;");
                }
                
                classBuilder
                    .Pad(1)
                    .Append("void Initialize(Func<string, IntPtr> getProcAddress");
                foreach (var kv in contexts.OrderBy(x => x.Value))
                { 
                    classBuilder
                        .Append(", ")
                        .Append(kv.Key)
                        .Append(" ")
                        .Append(GetContextNameFromIndex(kv.Value));
                }

                classBuilder.AppendLine(")").Pad(1).AppendLine("{");
                classBuilder.Append(initializeBody.ToString());
                classBuilder.Append("}\n}");


                context.AddSource(typeGroup.Key.GetFullyQualifiedName().Replace(":", ""), classBuilder.ToString());
            }
        });

        
    }

    static StringBuilder AppendRefKind(StringBuilder sb, RefKind kind)
    {
        if (kind == RefKind.Ref)
            sb.Append("ref ");
        if (kind == RefKind.Out)
            sb.Append("out ");
        return sb;
    }

    static bool NeedsPin(ITypeSymbol type, RefKind refKind)
    {
        if (type.TypeKind == TypeKind.Array || refKind != RefKind.None)
            return true;
        return false;
    }

    static string ConvertToNative(string name, ITypeSymbol type, RefKind refKind)
    {
        if (NeedsPin(type, refKind))
            return "__p_" + name;
        if (IsBool(type))
            return $"{name} ? 1 : 0";
        return name;
    }

    static string ConvertToManaged(ITypeSymbol type, string expr)
    {
        if (IsBool(type))
            return expr + " != 0";
        return expr;
    }

    static bool IsBool(ITypeSymbol type) => type.GetFullyQualifiedName() == "global::System.Boolean" ||
                                            type.GetFullyQualifiedName() == "bool";
    
    static string MapToNative(ITypeSymbol type, RefKind refKind)
    {
        if (type.TypeKind == TypeKind.Array)
            return ((IArrayTypeSymbol)type).ElementType.GetFullyQualifiedName() + "*";
        else if (refKind != RefKind.None)
            return MapToNative(type, RefKind.None) + "*";
        if (IsBool(type))
            return "int";
        return type.GetFullyQualifiedName();
    }

    static string BuildDelegateType(StringBuilder classBuilder, IMethodSymbol method, string fieldName)
    {
        StringBuilder functionPointer = new("delegate* unmanaged[Stdcall]<");
        // We need this one because Mono interpreter needs pre-generated trampolines for function pointers,
        // but .NET WASM SDK doesn't actually scan method bodies for calli instructions and only
        // looks for methods with DllImport and delegates with UnmanagedFunctionPointer
        StringBuilder fakeDelegate = new(
            "    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]\n    internal delegate ");
        fakeDelegate
            .Append(MapToNative(method.ReturnType, RefKind.None))
            .Append(" __wasmDummy")
            .Append(method.Name)
            .Append("(");


        int arg = 0;

        void AppendArgCore(StringBuilder builder, string a, bool isFirstArg)
        {
            if (!isFirstArg)
                builder.Append(",");
            builder.Append(a);
        }

        void AppendArg(string a, bool returnArg = false)
        {
            AppendArgCore(functionPointer, a, arg == 0);
            if (!returnArg)
            {
                AppendArgCore(fakeDelegate, a, arg == 0);
                fakeDelegate.Append($" a{arg}");
            }

            arg++;
        }

        foreach (var p in method.Parameters)
        {
            AppendArg(MapToNative(p.Type, p.RefKind));
        }

        AppendArg(MapToNative(method.ReturnType, RefKind.None), true);
        functionPointer.Append(">");
        fakeDelegate.Append(");");
        
        classBuilder
            .Pad(1)
            .Append(functionPointer);
        classBuilder
            .Append(fieldName)
            .AppendLine(";");
        classBuilder.AppendLine(fakeDelegate.ToString());
        return functionPointer.ToString();
    }

}
