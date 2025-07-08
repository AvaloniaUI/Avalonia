using Microsoft.CodeAnalysis.Text;

namespace Avalonia.Generators.NameGenerator;

internal interface INameGenerator
{
    public GeneratedPartialClass? GenerateNameReferences(SourceText sourceText);
}

internal record GeneratedPartialClass(string FileName, string Content);
