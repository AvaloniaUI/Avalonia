using System.IO;
using System.Linq;
using System.Text;
using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevGenerators;

[Generator(LanguageNames.CSharp)]
public class EnumMemberDictionaryGenerator : IIncrementalGenerator
{
    const string DictionaryAttributeFullName = "global::Avalonia.SourceGenerator.GenerateEnumValueDictionaryAttribute";
    const string ListAttributeFullName = "global::Avalonia.SourceGenerator.GenerateEnumValueListAttribute";
    
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

        var all = allMethodsWithAttributes
            .Where(s =>
                s.HasAttributeWithFullyQualifiedName(DictionaryAttributeFullName)
                || s.HasAttributeWithFullyQualifiedName(ListAttributeFullName)
            ).Collect();
        context.RegisterSourceOutput(all, static (context, methods) =>
        {
            foreach (var typeGroup in methods.GroupBy<IMethodSymbol,ISymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                var classBuilder = new StringBuilder();
                if (typeGroup.Key.ContainingNamespace != null)
                    classBuilder
                        .AppendLine("using System;")
                        .Append("namespace ")
                        .Append(typeGroup.Key.ContainingNamespace)
                        .AppendLine(";");
                classBuilder
                    .Append("partial class ")
                    .AppendLine(typeGroup.Key.Name)
                    .AppendLine("{");

                foreach (var method in typeGroup)
                {
                    var namedReturn = method.ReturnType as INamedTypeSymbol;
                    var arrayReturn = method.ReturnType as IArrayTypeSymbol;
                    
                    if ((namedReturn != null && namedReturn.Arity > 0) || arrayReturn != null)
                    {
                        ITypeSymbol enumType = namedReturn != null
                            ? namedReturn.TypeArguments.Last()
                            : arrayReturn!.ElementType;

                        var isDic = method.HasAttributeWithFullyQualifiedName(DictionaryAttributeFullName);

                        classBuilder
                            .Pad(1)
                            .Append("private static partial " + method.ReturnType + " " + method.Name + "()")
                            .AppendLine().Pad(4).Append(" => new ").Append(method.ReturnType).AppendLine("{");
                        foreach (var member in enumType.GetMembers())
                        {
                            if (member.Name == ".ctor")
                                continue;

                            if (isDic)
                                classBuilder.Pad(2)
                                    .Append("{\"")
                                    .Append(member.Name)
                                    .Append("\", ")
                                    .Append(member.ToString())
                                    .AppendLine("},");
                            else
                                classBuilder.Pad(2).Append(member.ToString()).AppendLine(",");
                        }

                        classBuilder.Pad(1).AppendLine("};");
                    }
                }
                classBuilder.AppendLine("}");

                context.AddSource(typeGroup.Key.GetFullyQualifiedName().Replace(":", ""), classBuilder.ToString());
            }
        });

        
    }

}
