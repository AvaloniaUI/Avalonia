using System;
using XamlX.Ast;
using XamlX.IL;
using XamlX.TypeSystem;
#nullable enable

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal class XamlDocumentResource : IXamlDocumentResource
{
    public XamlDocumentResource(
        XamlDocument xamlDocument,
        string? uri,
        IFileSource? fileSource,
        IXamlType? classType,
        bool isPublic,
        IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
        IXamlMethodBuilder<IXamlILEmitter> populateMethod,
        IXamlMethodBuilder<IXamlILEmitter>? buildMethod)
    {
        XamlDocument = xamlDocument;
        Uri = uri;
        FileSource = fileSource;
        ClassType = classType;
        IsPublic = isPublic;
        TypeBuilder = typeBuilder;
        PopulateMethod = populateMethod;
        BuildMethod = buildMethod;
    }

    public XamlDocument XamlDocument { get; }
    public string? Uri { get; }
    public IFileSource? FileSource { get; }

    public IXamlType? ClassType { get; }
    public bool IsPublic { get; }
    public IXamlTypeBuilder<IXamlILEmitter> TypeBuilder { get; }
    public IXamlMethodBuilder<IXamlILEmitter> PopulateMethod { get; }
    public IXamlMethodBuilder<IXamlILEmitter>? BuildMethod { get; }

    IXamlMethod? IXamlDocumentResource.BuildMethod => BuildMethod;
    IXamlMethod IXamlDocumentResource.PopulateMethod => PopulateMethod;
}
