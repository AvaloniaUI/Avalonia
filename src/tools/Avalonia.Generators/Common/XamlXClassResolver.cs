using Avalonia.Generators.Common.Domain;
using XamlX.Ast;

namespace Avalonia.Generators.Common;

internal class XamlXClassResolver : IClassResolver, IXamlAstVisitor
{
    public ResolvedClass? ResolveClass(XamlDocument xaml)
    {
        return null;
    }

    public IXamlAstNode Visit(IXamlAstNode node)
    {
        throw new System.NotImplementedException();
    }

    public void Push(IXamlAstNode node)
    {
        throw new System.NotImplementedException();
    }

    public void Pop()
    {
        throw new System.NotImplementedException();
    }
}
