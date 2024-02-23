using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Visitors;

internal class NameScopeRegistrationVisitor : Dictionary<string, (IXamlType type, IXamlLineInfo line)>, IXamlAstVisitor
{
    private int _metadataScopeLevel;
    private Stack<IXamlAstNode> _parents = new();

    public NameScopeRegistrationVisitor(int initialMetadataScopeLevel = 0)
    {
        _metadataScopeLevel = initialMetadataScopeLevel;
    }
    
    IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
    {
        if (_metadataScopeLevel == 1
            && node is AvaloniaNameScopeRegistrationXamlIlNode nameScopeRegistration
            && nameScopeRegistration.Name is XamlAstTextNode textNode)
        {
            this[textNode.Text] = (nameScopeRegistration.TargetType, textNode);
        }

        return node;
    }

    void IXamlAstVisitor.Push(IXamlAstNode node)
    {
        _parents.Push(node);
        if (node is NestedScopeMetadataNode)
        {
            _metadataScopeLevel++;
        }
    }

    void IXamlAstVisitor.Pop()
    {
        var oldParent = _parents.Pop();
        if (oldParent is NestedScopeMetadataNode)
        {
            _metadataScopeLevel--;
        }
    }
}
