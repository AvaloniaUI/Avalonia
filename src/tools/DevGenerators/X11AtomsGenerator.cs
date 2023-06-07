using System.IO;
using System.Linq;
using System.Text;
using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevGenerators;

[Generator(LanguageNames.CSharp)]
public class X11AtomsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var x11AtomsClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => s is ClassDeclarationSyntax
                {
                    Identifier.Text: "X11Atoms"
                },
                static (context, _) =>
                    (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!);

        var all = x11AtomsClasses.Collect();
        context.RegisterSourceOutput(all, static (context, classes) =>
        {
            foreach (var cl in classes)
            {
                var classBuilder = new StringBuilder();
                if (cl.ContainingNamespace != null)
                    classBuilder
                        .AppendLine("using System;")
                        .AppendLine("using static Avalonia.X11.XLib;")
                        .Append("namespace ")
                        .Append(cl.ContainingNamespace)
                        .AppendLine(";");
                classBuilder
                    .Append("partial class ")
                    .AppendLine(cl.Name)
                    .AppendLine("{");

                var fields = cl.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => f.Type.Name == "IntPtr"
                                && f.DeclaredAccessibility == Accessibility.Public).ToList();
                
                classBuilder.Pad(1).AppendLine("private void PopulateAtoms(IntPtr display)").Pad(1).AppendLine("{");
                classBuilder.Pad(2).Append("var atoms = new IntPtr[").Append(fields.Count).AppendLine("];");
                classBuilder.Pad(2).Append("var atomNames = new string[").Append(fields.Count).AppendLine("] {");


                for (int c = 0; c < fields.Count; c++)
                    classBuilder.Pad(3).Append("\"").Append(fields[c].Name).AppendLine("\",");
                classBuilder.Pad(2).AppendLine("};");
                
                classBuilder.Pad(2).AppendLine("XInternAtoms(display, atomNames, atomNames.Length, true, atoms);");

                for (int c = 0; c < fields.Count; c++)
                    classBuilder.Pad(2).Append("InitAtom(ref ").Append(fields[c].Name).Append(", \"")
                        .Append(fields[c].Name).Append("\", atoms[").Append(c).AppendLine("]);");


                classBuilder.Pad(1).AppendLine("}");
                classBuilder.AppendLine("}");

                context.AddSource(cl.GetFullyQualifiedName().Replace(":", ""), classBuilder.ToString());
            }
        });

        
    }

}