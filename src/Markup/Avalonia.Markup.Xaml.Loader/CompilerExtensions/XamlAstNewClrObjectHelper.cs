using XamlX.Ast;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

internal static class XamlAstNewClrObjectHelper
{
    /// <summary>
    /// Tries to resolve the underlying value of a <see cref="XamlValueWithManipulationNode"/>,
    /// unwrapping any nested <see cref="XamlValueWithManipulationNode"/> instances.
    /// </summary>
    public static TXamlAstValueNode? UnwrapValue<TXamlAstValueNode>(this XamlValueWithManipulationNode node)
        where TXamlAstValueNode : class, IXamlAstValueNode
    {
        var current = node.Value;
        while (current is XamlValueWithManipulationNode valueWithManipulation)
        {
            current = valueWithManipulation.Value;
            if (current is TXamlAstValueNode typedValue)
            {
                return typedValue;
            }
        }

        return current as TXamlAstValueNode;
    }
}
