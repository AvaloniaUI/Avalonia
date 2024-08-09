using XamlX;
using XamlX.Ast;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Visitors;

class NotSharedVisitor : IXamlAstVisitor
{
    public int Count { get; private set; }
    private int level = 0;

    public void Pop()
    {
        level--;
    }

    public void Push(IXamlAstNode node)
    {
        level++;
    }

    public IXamlAstNode Visit(IXamlAstNode node)
    {
        if (TryGetXShared(node, out var isShared))
        {
            if (!isShared)
            {
                Count++;
                return node;
            }
        }
        else if (level < 3)
        {
            node.VisitChildren(this);
        }
        return  node;
    }

    private static bool TryGetXShared(IXamlAstNode node, out bool isShared)
    {
        isShared = false;
        if (node is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: "Shared" } sharedDirective)
        {
            if (sharedDirective.Values.Count == 1 && sharedDirective.Values[0] is XamlAstTextNode text)
            {
                if (bool.TryParse(text.Text, out var value))
                {
                    isShared = value;
                    return true;
                }
            }
        }
        return false;
    }
}

