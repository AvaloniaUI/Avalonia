using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;

namespace Avalonia.Generators.Common;

internal class XamlXViewResolver : IViewResolver, IXamlAstVisitor
{
    private readonly RoslynTypeSystem _typeSystem;
    private readonly MiniCompiler _compiler;
    private readonly bool _checkTypeValidity;
    private readonly Action<string> _onTypeInvalid;
    private readonly Action<Exception> _onUnhandledError;

    private ResolvedView _resolvedClass;
    private XamlDocument _xaml;

    public XamlXViewResolver(
        RoslynTypeSystem typeSystem,
        MiniCompiler compiler,
        bool checkTypeValidity = false,
        Action<string> onTypeInvalid = null,
        Action<Exception> onUnhandledError = null)
    {
        _checkTypeValidity = checkTypeValidity;
        _onTypeInvalid = onTypeInvalid;
        _onUnhandledError = onUnhandledError;
        _typeSystem = typeSystem;
        _compiler = compiler;
    }

    public ResolvedView ResolveView(string xaml)
    {
        try
        {
            _resolvedClass = null;
            _xaml = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            _compiler.Transform(_xaml);
            _xaml.Root.Visit(this);
            _xaml.Root.VisitChildren(this);
            return _resolvedClass;
        }
        catch (Exception exception)
        {
            _onUnhandledError?.Invoke(exception);
            return null;
        }
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
            if (child is XamlAstXmlDirective directive &&
                directive.Name == "Class" &&
                directive.Namespace == XamlNamespaces.Xaml2006 &&
                directive.Values[0] is XamlAstTextNode text)
            {
                if (_checkTypeValidity)
                {
                    var existingType = _typeSystem.FindType(text.Text);
                    if (existingType == null)
                    {
                        _onTypeInvalid?.Invoke(text.Text);
                        return node;
                    }
                }

                var split = text.Text.Split('.');
                var nameSpace = string.Join(".", split.Take(split.Length - 1));
                var className = split.Last();

                _resolvedClass = new ResolvedView(className, clrType, nameSpace, _xaml);
                return node;
            }
        }

        return node;
    }

    void IXamlAstVisitor.Push(IXamlAstNode node) { }

    void IXamlAstVisitor.Pop() { }
}
