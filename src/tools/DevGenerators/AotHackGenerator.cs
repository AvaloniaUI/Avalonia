using System.Linq;
using System.Text;
using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevGenerators;

[Generator(LanguageNames.CSharp)]
public class AotHackGeneratorls : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        return;
        var x11AtomsClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) =>
                {
                    if (s is FieldDeclarationSyntax fieldDec)
                    {
                        foreach (var v in fieldDec.Declaration.Variables)
                            if (v.Identifier.Text.EndsWith("Property")
                                && char.IsUpper(v.Identifier.Text[0])
                               )
                            {
                                foreach (var m in fieldDec.Modifiers)
                                    return m.Text == "public";
                            }
                    }

                    return false;
                },
                static (context, _) =>
                    (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(
                        ((FieldDeclarationSyntax)context.Node).Declaration.Variables[0])!);

        var all = x11AtomsClasses.Collect();
        context.RegisterSourceOutput(all, static (context, vars) =>
        {
            var classBuilder = new StringBuilder()
                .AppendLine("namespace AotHack;")
                .AppendLine("static class AotHackInit")
                .AppendLine("{")
                .Pad(1).AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]")
                .Pad(1).AppendLine("public static void Init()")
                .Pad(1).AppendLine("{");
            foreach (var v in vars)
            {
                if (v != null && v.ContainingType.Arity == 0)
                    classBuilder.Pad(2).Append("System.GC.KeepAlive(").Append(v).AppendLine(");");
            }

            classBuilder.Pad(1).AppendLine("}");
            classBuilder.AppendLine("}");

            context.AddSource("AOT_HACK", classBuilder.ToString());
        });
    }

}