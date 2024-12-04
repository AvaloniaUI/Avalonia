using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Visitors;

internal class NameScopeRegistrationVisitor : Dictionary<string, (IXamlType type, IXamlLineInfo line)>, IXamlAstVisitor
{
    private readonly int _targetMetadataScopeLevel;
    private readonly Stack<IXamlAstNode> _parents = new();
    private int _metadataScopeLevel;

    public NameScopeRegistrationVisitor(
        int initialMetadataScopeLevel = 0,
        int targetMetadataScopeLevel = 1)
    {
        _metadataScopeLevel = initialMetadataScopeLevel;
        _targetMetadataScopeLevel = targetMetadataScopeLevel;
    }
    
    IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
    {
        if (_metadataScopeLevel == _targetMetadataScopeLevel
            && node is AvaloniaNameScopeRegistrationXamlIlNode nameScopeRegistration
            && nameScopeRegistration.Name is XamlAstTextNode textNode)
        {
            this[textNode.Text] = (nameScopeRegistration.TargetType ?? XamlPseudoType.Unknown, textNode);
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
