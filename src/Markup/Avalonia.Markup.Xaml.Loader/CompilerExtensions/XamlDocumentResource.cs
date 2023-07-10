using System;
using XamlX.Ast;
using XamlX.TypeSystem;
#nullable enable

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal class XamlDocumentResource : IXamlDocumentResource
{
    private readonly Func<XamlDocumentTypeBuilderProvider> _createTypeBuilderProvider;
    private XamlDocumentTypeBuilderProvider? _typeBuilderProvider;

    public XamlDocumentResource(
        XamlDocument xamlDocument,
        string? uri,
        IFileSource? fileSource,
        IXamlType? classType,
        bool isPublic,
        Func<XamlDocumentTypeBuilderProvider> createTypeBuilderProvider)
    {
        _createTypeBuilderProvider = createTypeBuilderProvider;
        XamlDocument = xamlDocument;
        Uri = uri;
        FileSource = fileSource;
        ClassType = classType;
        IsPublic = isPublic;
    }

    public XamlDocument XamlDocument { get; }
    public string? Uri { get; }
    public IFileSource? FileSource { get; }

    public IXamlType? ClassType { get; }
    public bool IsPublic { get; }
    public XamlDocumentUsage Usage { get; set; }

    public XamlDocumentTypeBuilderProvider TypeBuilderProvider
    {
        get
        {
            if (_typeBuilderProvider is null)
            {
                _typeBuilderProvider = _createTypeBuilderProvider();
                Usage = XamlDocumentUsage.Used;
            }

            return _typeBuilderProvider;
        }
    }

    IXamlMethod? IXamlDocumentResource.BuildMethod => TypeBuilderProvider.BuildMethod;
    IXamlMethod IXamlDocumentResource.PopulateMethod => TypeBuilderProvider.PopulateMethod;
}
