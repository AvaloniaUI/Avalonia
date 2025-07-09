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

internal class XamlXViewResolver(
    IXamlTypeSystem typeSystem,
    MiniCompiler compiler,
    bool checkTypeValidity = false,
    Action<string>? onTypeInvalid = null) : IViewResolver, IXamlAstVisitor
{
    private ResolvedView? _resolvedClass;
    private XamlDocument? _xaml;

    public ResolvedView? ResolveView(string xaml)
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

        var clrType = objectNode.Type.GetClrType();
        if (!clrType.IsAvaloniaStyledElement())
            return node;
        foreach (var child in objectNode.Children)
        {
            if (child is XamlAstXmlDirective { Name: "Class", Namespace: XamlNamespaces.Xaml2006 } directive
                && directive.Values[0] is XamlAstTextNode text)
            {
                var existingType = typeSystem.FindType(text.Text);
                if (checkTypeValidity && existingType == null)
                {
                    onTypeInvalid?.Invoke(text.Text);
                    return node;
                }

                var split = text.Text.Split('.');
                var nameSpace = string.Join(".", split.Take(split.Length - 1));
                var className = split.Last();

                _resolvedClass = new ResolvedView(className, nameSpace, existingType?.IsAvaloniaWindow() ?? false, _xaml!);
                return node;
            }
        }

        return node;
    }

    void IXamlAstVisitor.Push(IXamlAstNode node) { }

    void IXamlAstVisitor.Pop() { }
}
