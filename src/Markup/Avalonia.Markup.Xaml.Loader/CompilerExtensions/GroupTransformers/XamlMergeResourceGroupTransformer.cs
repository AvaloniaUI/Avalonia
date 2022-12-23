using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.IL.Emitters;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;
#nullable enable

internal class XamlMergeResourceGroupTransformer : IXamlAstGroupTransformer
{
    public IXamlAstNode Transform(AstGroupTransformationContext context, IXamlAstNode node)
    {
        var resourceDictionaryType = context.GetAvaloniaTypes().ResourceDictionary;
        if (node is not XamlObjectInitializationNode resourceDictionaryNode
            || resourceDictionaryNode.Type != resourceDictionaryType
            || resourceDictionaryNode.Manipulation is not XamlManipulationGroupNode resourceDictionaryManipulation)
        {
            return node;
        }

        var mergeResourceIncludeType = context.GetAvaloniaTypes().MergeResourceInclude;
        var mergeSourceNodes = new List<XamlPropertyAssignmentNode>();
        var hasAnyNonMergedResource = false;
        foreach (var manipulationNode in resourceDictionaryManipulation.Children.ToArray())
        {
            void ProcessXamlPropertyAssignmentNode(XamlManipulationGroupNode parent, XamlPropertyAssignmentNode assignmentNode)
            {
                if (assignmentNode.Property.Name == "MergedDictionaries"
                    && assignmentNode.Values.FirstOrDefault() is XamlValueWithManipulationNode valueNode)
                {
                    if (valueNode.Type.GetClrType() == mergeResourceIncludeType)
                    {
                        if (valueNode.Manipulation is XamlObjectInitializationNode objectInitialization
                            && objectInitialization.Manipulation is XamlPropertyAssignmentNode sourceAssignmentNode)
                        {
                            parent.Children.Remove(assignmentNode);
                            mergeSourceNodes.Add(sourceAssignmentNode);   
                        }
                        else
                        {
                            throw new XamlDocumentParseException(context.CurrentDocument,
                                "Invalid MergeResourceInclude node found. Make sure that Source property is set.",
                                valueNode);
                        }
                    }
                    else
                    {
                        hasAnyNonMergedResource = true;
                    }

                    if (hasAnyNonMergedResource && mergeSourceNodes.Any())
                    {
                        throw new XamlDocumentParseException(context.CurrentDocument,
                            "Mix of MergeResourceInclude and other dictionaries inside of the ResourceDictionary.MergedDictionaries is not allowed",
                            valueNode);
                    }
                }
            }
            
            if (manipulationNode is XamlPropertyAssignmentNode singleValueAssignment)
            {
                ProcessXamlPropertyAssignmentNode(resourceDictionaryManipulation, singleValueAssignment);
            }
            else if (manipulationNode is XamlManipulationGroupNode groupNodeValues)
            {
                foreach (var groupNodeValue in groupNodeValues.Children.OfType<XamlPropertyAssignmentNode>().ToArray())
                {
                    ProcessXamlPropertyAssignmentNode(groupNodeValues, groupNodeValue);
                }
            }
        }

        var manipulationGroup = new XamlManipulationGroupNode(node, new List<IXamlAstManipulationNode>());
        foreach (var sourceNode in mergeSourceNodes)
        {
            var (originalAssetPath, propertyNode) =
                AvaloniaXamlIncludeTransformer.ResolveSourceFromXamlInclude(context, "MergeResourceInclude", sourceNode, true);
            if (originalAssetPath is null)
            {
                return node;
            }
            
            var targetDocument = context.Documents.FirstOrDefault(d =>
                    string.Equals(d.Uri, originalAssetPath, StringComparison.InvariantCultureIgnoreCase))
                ?.XamlDocument.Root as XamlValueWithManipulationNode;
            if (targetDocument is null)
            {
                return context.ParseError(
                    $"Node MergeResourceInclude is unable to resolve \"{originalAssetPath}\" path.", propertyNode, node);
            }

            var singleRootObject = ((XamlManipulationGroupNode)targetDocument.Manipulation)
                .Children.OfType<XamlObjectInitializationNode>().Single();
            if (singleRootObject.Type != resourceDictionaryType)
            {
                return context.ParseError(
                    $"MergeResourceInclude can only include another ResourceDictionary", propertyNode, node);
            }
            
            manipulationGroup.Children.Add(singleRootObject.Manipulation);
        }

        if (manipulationGroup.Children.Any())
        {
            // MergedDictionaries are read first, so we need ot inject our merged values in the beginning.
            resourceDictionaryManipulation.Children.Insert(0, manipulationGroup);
        }
        
        return node;
    }
}
