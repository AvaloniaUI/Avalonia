using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common;

internal class XamlXViewResolver(MiniCompiler compiler) : IViewResolver, IXamlAstVisitor
{
    private ResolvedViewDocument? _resolvedClass;
    private XamlDocument? _xaml;

    public ResolvedViewDocument? ResolveView(string xaml)
    {
        _resolvedClass = null;
        _xaml = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
        {
            {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
        });

        compiler.Transform(_xaml);
        _xaml.Root.Visit(this);
        _xaml.Root.VisitChildren(this);
        return _resolvedClass;
    }

    IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
    {
        if (node is not XamlAstObjectNode objectNode)
            return node;

        foreach (var child in objectNode.Children)
        {
            if (child is XamlAstXmlDirective { Name: "Class", Namespace: XamlNamespaces.Xaml2006 } directive
                && directive.Values[0] is XamlAstTextNode text)
            {
                var split = text.Text.Split('.');
                var nameSpace = string.Join(".", split.Take(split.Length - 1));
                var className = split.Last();

                _resolvedClass = new ResolvedViewDocument(className, nameSpace, _xaml!);
                return node;
            }
        }

        return node;
    }

    void IXamlAstVisitor.Push(IXamlAstNode node) { }

    void IXamlAstVisitor.Pop() { }
}
