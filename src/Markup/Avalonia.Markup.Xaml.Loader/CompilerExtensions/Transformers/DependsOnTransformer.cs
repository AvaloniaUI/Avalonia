using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class DependsOnTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlManipulationGroupNode manipulationGroupNode)
        {
            var list = manipulationGroupNode.Children;
            var dependsOnAttribute = context.GetAvaloniaTypes().DependsOnAttribute;
            manipulationGroupNode.Children =
                TSort(list, x =>
                {
                    if (x is XamlPropertyAssignmentNode assignmentNode
                        && assignmentNode.Property is XamlIlAvaloniaProperty avaloniaProperty)
                    {
                        var dependsOnPropertiesName = avaloniaProperty.CustomAttributes
                         .Where(att => att.Type == dependsOnAttribute)
                         .Select(att => att.Parameters[0])
                         .ToArray();
                        return list
                            .Where(i => i is XamlPropertyAssignmentNode ap && dependsOnPropertiesName.Contains(ap.Property.Name));
                    }
                    return Array.Empty<IXamlAstManipulationNode>();
                }).ToList();
        }
        return node;
    }

    public static IEnumerable<IXamlAstManipulationNode> TSort<IXamlAstManipulationNode>(IEnumerable<IXamlAstManipulationNode> source, Func<IXamlAstManipulationNode, IEnumerable<IXamlAstManipulationNode>> dependencies, bool throwOnCycle = false)
    {
        var sorted = new List<IXamlAstManipulationNode>();
        var visited = new HashSet<IXamlAstManipulationNode>();

        foreach (var item in source)
            Visit(item, visited, sorted, dependencies, throwOnCycle);

        return sorted;
    }

    private static void Visit<IXamlAstManipulationNode>(IXamlAstManipulationNode item, HashSet<IXamlAstManipulationNode> visited, List<IXamlAstManipulationNode> sorted, Func<IXamlAstManipulationNode, IEnumerable<IXamlAstManipulationNode>> dependencies, bool throwOnCycle)
    {
        if (!visited.Contains(item))
        {
            visited.Add(item);

            foreach (var dep in dependencies(item))
                Visit(dep, visited, sorted, dependencies, throwOnCycle);

            sorted.Add(item);
        }
        else
        {
            if (throwOnCycle && !sorted.Contains(item))
                throw new Exception("Cyclic dependency found");
        }
    }


}
