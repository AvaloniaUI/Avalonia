using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Avalonia.Generators.SceneGenerator
{
    [Generator(LanguageNames.CSharp)]
    internal sealed class AvaloniaSceneIncrementalGenerator : IIncrementalGenerator
    {
        private const string SceneAttributeMetadataName = "Avalonia.Controls.SceneAppAttribute`1";
        private const string AndroidSceneAttributeMetadataName = "Avalonia.Controls.AndroidSceneAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var sceneClasses = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    SceneAttributeMetadataName,
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (context, _) => (INamedTypeSymbol)context.TargetSymbol)
                .WithTrackingName("SceneAppClasses");

            context.RegisterSourceOutput(sceneClasses.Collect().Combine(context.CompilationProvider), static (context, pair) =>
            {
                var analyzedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
                foreach (var sceneAppClass in pair.Left)
                {
                    AnalyzeAttributedClass(context, pair.Right, sceneAppClass, analyzedAssemblies);
                }
            });

            context.RegisterSourceOutput(context.CompilationProvider, static (context, compilation) =>
            {
                AnalyzeReferencedSceneClasses(context, compilation);
            });
        }

        private static void AnalyzeReferencedSceneClasses(SourceProductionContext context, Compilation compilation)
        {
            var sceneAttribute = compilation.GetTypeByMetadataName(SceneAttributeMetadataName);
            var sceneType = compilation.GetTypeByMetadataName("Avalonia.Controls.Scene");
            if (sceneAttribute is null || sceneType is null)
            {
                return;
            }

            var analyzedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    AnalyzeAttributedClasses(context, assembly.GlobalNamespace, sceneAttribute, sceneType, analyzedAssemblies);
                }
            }
        }

        private static void AnalyzeAttributedClasses(
            SourceProductionContext context,
            INamespaceSymbol namespaceSymbol,
            INamedTypeSymbol sceneAttribute,
            INamedTypeSymbol sceneType,
            HashSet<IAssemblySymbol> analyzedAssemblies)
        {
            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                AnalyzeAttributedClasses(context, nestedNamespace, sceneAttribute, sceneType, analyzedAssemblies);
            }

            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                AnalyzeAttributedClasses(context, type, sceneAttribute, sceneType, analyzedAssemblies);
            }
        }

        private static void AnalyzeAttributedClasses(
            SourceProductionContext context,
            INamedTypeSymbol type,
            INamedTypeSymbol sceneAttribute,
            INamedTypeSymbol sceneType,
            HashSet<IAssemblySymbol> analyzedAssemblies)
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.OriginalDefinition, sceneAttribute))
                {
                    AnalyzeSceneAppAttribute(context, attribute, type.ContainingAssembly, type.ContainingNamespace, sceneType, analyzedAssemblies);
                }
            }

            foreach (var nestedType in type.GetTypeMembers())
            {
                AnalyzeAttributedClasses(context, nestedType, sceneAttribute, sceneType, analyzedAssemblies);
            }
        }

        private static void AnalyzeAttributedClass(
            SourceProductionContext context,
            Compilation compilation,
            INamedTypeSymbol attributedClass,
            HashSet<IAssemblySymbol> analyzedAssemblies)
        {
            var sceneAttribute = compilation.GetTypeByMetadataName(SceneAttributeMetadataName);
            var sceneType = compilation.GetTypeByMetadataName("Avalonia.Controls.Scene");
            if (sceneAttribute is null || sceneType is null)
            {
                return;
            }

            foreach (var attribute in attributedClass.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.OriginalDefinition, sceneAttribute))
                {
                    AnalyzeSceneAppAttribute(context, attribute, attributedClass.ContainingAssembly, attributedClass.ContainingNamespace, sceneType, analyzedAssemblies);
                }
            }
        }

        private static void AnalyzeSceneAppAttribute(
            SourceProductionContext context,
            AttributeData attribute,
            IAssemblySymbol attributeAssembly,
            INamespaceSymbol attributedNamespace,
            INamedTypeSymbol sceneType,
            HashSet<IAssemblySymbol> analyzedAssemblies)
        {
            var attributeType = attribute.AttributeClass;
            if (attributeType is not null &&
                attributeType.TypeArguments.Length > 0 &&
                attributeType.TypeArguments[0] is INamedTypeSymbol targetType &&
                analyzedAssemblies.Add(targetType.ContainingAssembly))
            {
                AnalyzeAssemblyScenes(context, targetType.ContainingAssembly, attributeAssembly, attributedNamespace, sceneType);
            }
        }

        private static void AnalyzeAssemblyScenes(
            SourceProductionContext context,
            IAssemblySymbol assembly,
            IAssemblySymbol attributeAssembly,
            INamespaceSymbol attributedNamespace,
            INamedTypeSymbol sceneType)
        {
            var generatedActivityClassNames = new List<string>();
            AnalyzeNamespace(context, assembly.GlobalNamespace, attributeAssembly, attributedNamespace, sceneType, generatedActivityClassNames);
            GenerateSceneAppBuilderExtension(context, attributedNamespace, generatedActivityClassNames);
        }

        private static void AnalyzeNamespace(
            SourceProductionContext context,
            INamespaceSymbol namespaceSymbol,
            IAssemblySymbol attributeAssembly,
            INamespaceSymbol attributedNamespace,
            INamedTypeSymbol sceneType,
            List<string> generatedActivityClassNames)
        {
            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                AnalyzeNamespace(context, nestedNamespace, attributeAssembly, attributedNamespace, sceneType, generatedActivityClassNames);
            }

            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                AnalyzeType(context, type, attributeAssembly, attributedNamespace, sceneType, generatedActivityClassNames);
            }
        }

        private static void AnalyzeType(
            SourceProductionContext context,
            INamedTypeSymbol type,
            IAssemblySymbol attributeAssembly,
            INamespaceSymbol attributedNamespace,
            INamedTypeSymbol sceneType,
            List<string> generatedActivityClassNames)
        {
            if (type.TypeKind == TypeKind.Class && !type.IsAbstract && IsSceneType(type, sceneType))
            {
                var androidSceneAttribute = FindAndroidSceneAttribute(type);
                if (androidSceneAttribute is not null)
                {
                    AnalyzeSceneClass(context, type, attributeAssembly, attributedNamespace, androidSceneAttribute, generatedActivityClassNames);
                }
            }

            foreach (var nestedType in type.GetTypeMembers())
            {
                AnalyzeType(context, nestedType, attributeAssembly, attributedNamespace, sceneType, generatedActivityClassNames);
            }
        }

        private static AttributeData? FindAndroidSceneAttribute(INamedTypeSymbol sceneClass)
        {
            foreach (var attribute in sceneClass.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == AndroidSceneAttributeMetadataName)
                {
                    return attribute;
                }
            }

            return null;
        }

        private static bool IsSceneType(INamedTypeSymbol type, INamedTypeSymbol sceneType)
        {
            for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, sceneType))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AnalyzeSceneClass(
            SourceProductionContext context,
            INamedTypeSymbol sceneClass,
            IAssemblySymbol attributeAssembly,
            INamespaceSymbol attributedNamespace,
            AttributeData androidSceneAttribute,
            List<string> generatedActivityClassNames)
        {
            var label = androidSceneAttribute.ConstructorArguments.Length > 0
                ? androidSceneAttribute.ConstructorArguments[0].Value as string ?? string.Empty
                : string.Empty;
            var androidTheme = androidSceneAttribute.ConstructorArguments.Length > 1
                ? androidSceneAttribute.ConstructorArguments[1].Value as string ?? string.Empty
                : string.Empty;
            var iconResource = androidSceneAttribute.ConstructorArguments.Length > 2
                ? androidSceneAttribute.ConstructorArguments[2].Value as string ?? string.Empty
                : string.Empty;
            var isExported = androidSceneAttribute.ConstructorArguments.Length > 3 &&
                androidSceneAttribute.ConstructorArguments[3].Value is true;
            var isLauncher = androidSceneAttribute.ConstructorArguments.Length > 4 &&
                androidSceneAttribute.ConstructorArguments[4].Value is true;
            var configChanges = androidSceneAttribute.ConstructorArguments.Length > 5 &&
                androidSceneAttribute.ConstructorArguments[5].Value is int configChangesValue
                    ? configChangesValue
                    : 128 | 1024 | 512 | 2048;
            var sceneClassName = sceneClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var generatedClassName = $"{sceneClass.Name}Activity";
            var namespaceName = attributedNamespace.ToDisplayString();
            var hintName = string.IsNullOrEmpty(namespaceName)
                ? $"{generatedClassName}.g.cs"
                : $"{namespaceName}.{generatedClassName}.g.cs";
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine("using Android.App;");
            sourceBuilder.AppendLine("using Android.Content.PM;");
            sourceBuilder.AppendLine("using Avalonia.Android;");
            sourceBuilder.AppendLine("using Avalonia.Android.Scene;");
            sourceBuilder.AppendLine("using Android.OS;");
            sourceBuilder.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sourceBuilder.Append("namespace ").Append(namespaceName).AppendLine(";").AppendLine();
            }

            sourceBuilder.Append("[Activity(Label = ").Append(SymbolDisplay.FormatLiteral(label, quote: true))
                .Append(", Theme = ").Append(SymbolDisplay.FormatLiteral(androidTheme, quote: true))
                .Append(", Icon = ").Append(SymbolDisplay.FormatLiteral(iconResource, quote: true))
                .Append(", Exported = ").Append(isExported ? "true" : "false")
                .Append(", MainLauncher = ").Append(isLauncher ? "true" : "false")
                .Append(", ConfigurationChanges = (ConfigChanges)").Append(configChanges).AppendLine(")]");
            var generatedClassFullName = string.IsNullOrEmpty(namespaceName)
                ? $"global::{generatedClassName}"
                : $"global::{namespaceName}.{generatedClassName}";
            generatedActivityClassNames.Add(generatedClassFullName);

            sourceBuilder.Append("public class ").Append(generatedClassName)
                .Append(" : global::Avalonia.Android.Scene.SceneActivity<")
                .Append(sceneClassName).AppendLine(">")
                .AppendLine("{")
                .AppendLine("    internal static void RegisterInManager()")
                .AppendLine("    {")
                .Append("        RegisterInManager<").Append(sceneClassName).Append(">(typeof(")
                .Append(generatedClassFullName).AppendLine("));")
                .AppendLine("    }")
                .AppendLine("}");

            context.AddSource(hintName, SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static void GenerateSceneAppBuilderExtension(
            SourceProductionContext context,
            INamespaceSymbol attributedNamespace,
            List<string> generatedActivityClassNames)
        {
            var namespaceName = attributedNamespace.ToDisplayString();
            var hintName = string.IsNullOrEmpty(namespaceName)
                ? "SceneAppBuilderExtension.g.cs"
                : $"{namespaceName}.SceneAppBuilderExtension.g.cs";
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine("using Avalonia;");
            sourceBuilder.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sourceBuilder.Append("namespace ").Append(namespaceName).AppendLine(";").AppendLine();
            }

            sourceBuilder.AppendLine("internal static class SceneAppBuilderExtension")
                .AppendLine("{")
                .AppendLine("    public static AppBuilder UseScenes(this AppBuilder builder)")
                .AppendLine("    {")
                .AppendLine("        return builder.UseAndroidScene().AfterSetup(_ =>")
                .AppendLine("        {");

            foreach (var generatedActivityClassName in generatedActivityClassNames)
            {
                sourceBuilder.Append("            ").Append(generatedActivityClassName).AppendLine(".RegisterInManager();");
            }

            sourceBuilder.AppendLine("        });")
                .AppendLine("    }")
                .AppendLine("}");

            context.AddSource(hintName, SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}
