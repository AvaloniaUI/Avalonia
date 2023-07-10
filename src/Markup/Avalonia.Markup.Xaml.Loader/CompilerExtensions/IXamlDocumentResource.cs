using XamlX.Ast;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

#nullable enable

internal interface IXamlDocumentResource
{
    IXamlMethod? BuildMethod { get; }
    IXamlType? ClassType { get; }
    string? Uri { get; }
    IXamlMethod PopulateMethod { get; }
    IFileSource? FileSource { get; }
    XamlDocument XamlDocument { get; }
    XamlDocumentUsage Usage { get; set; }
}
