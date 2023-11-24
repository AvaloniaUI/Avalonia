using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.TypeSystem;

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
        var shouldExit = false; // if any manipulation node has an error, we should stop processing further.
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
                            shouldExit = true;
                            context.ReportTransformError(
                                "Invalid MergeResourceInclude node found. Make sure that Source property is set.",
                                valueNode);
                        }
                    }
                    else if (mergeSourceNodes.Any())
                    {
                        shouldExit = true;
                        context.ReportTransformError(
                            "MergeResourceInclude should always be included last when mixing with other dictionaries inside of the ResourceDictionary.MergedDictionaries.",
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

        if (shouldExit || !mergeSourceNodes.Any())
        {
            return node;
        }
        
        var manipulationGroup = new List<IXamlAstManipulationNode>();
        foreach (var sourceNode in mergeSourceNodes)
        {
            var (originalAssetPath, propertyNode) =
                AvaloniaXamlIncludeTransformer.ResolveSourceFromXamlInclude(context, "MergeResourceInclude", sourceNode, true);
            if (originalAssetPath is null)
            {
                return context.ReportTransformError(
                    $"Node MergeResourceInclude is unable to resolve \"{originalAssetPath}\" path.", propertyNode, node);
            }

            var targetDocument = context.Documents.FirstOrDefault(d =>
                string.Equals(d.Uri, originalAssetPath, StringComparison.InvariantCultureIgnoreCase));
            if (targetDocument?.XamlDocument.Root is not XamlValueWithManipulationNode targetDocumentRoot)
            {
                return context.ReportTransformError(
                    $"Node MergeResourceInclude is unable to resolve \"{originalAssetPath}\" path.", propertyNode, node);
            }

            var singleRootObject = ((XamlManipulationGroupNode)targetDocumentRoot.Manipulation)
                .Children.OfType<XamlObjectInitializationNode>().Single();
            if (singleRootObject.Type != resourceDictionaryType)
            {
                return context.ReportTransformError(
                    "MergeResourceInclude can only include another ResourceDictionary", propertyNode, node);
            }
            
            manipulationGroup.Add(singleRootObject.Manipulation);

            if (targetDocument.Usage == XamlDocumentUsage.Unknown)
            {
                targetDocument.Usage = XamlDocumentUsage.Merged;
            }
        }
        
        // Order of resources is defined by ResourceDictionary.TryGetResource.
        // It is read by following priority:
        // - own resources.
        // - own theme dictionaries.
        // - merged dictionaries.
        // We need to maintain this order when we inject "compiled merged" resources.
        // Doing this by injecting merged dictionaries in the beginning, so it can be overwritten by "own resources".  
        // MergedDictionaries are read first, so we need ot inject our merged values in the beginning.
        var children = resourceDictionaryManipulation.Children;
        children.InsertRange(0, manipulationGroup);

        // Flatten resource assignments.
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is XamlManipulationGroupNode group)
            {
                children.RemoveAt(i);
                children.AddRange(group.Children);
                i--; // step back, so new items can be reiterated.
            }
        }

        // Merge "ThemeDictionaries" as well.
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is XamlPropertyAssignmentNode assignmentNode
                && assignmentNode.Property.Name == "ThemeDictionaries"
                && assignmentNode.Values.Count == 2
                && assignmentNode.Values[0] is {} key
                && assignmentNode.Values[1] is XamlValueWithManipulationNode
                {
                    Manipulation: XamlObjectInitializationNode
                    {
                        Manipulation: XamlManipulationGroupNode valueGroup
                    }
                })
            {
                for (var j = i - 1; j >= 0; j--)
                {
                    if (children[j] is XamlPropertyAssignmentNode sameKeyPrevAssignmentNode
                        && sameKeyPrevAssignmentNode.Property.Name == "ThemeDictionaries"
                        && sameKeyPrevAssignmentNode.Values.Count == 2
                        && sameKeyPrevAssignmentNode.Values[1] is XamlValueWithManipulationNode
                        {
                            Manipulation: XamlObjectInitializationNode
                            {
                                Manipulation: XamlManipulationGroupNode sameKeyPrevValueGroup
                            }
                        }
                        && ThemeVariantNodeEquals(context, key, sameKeyPrevAssignmentNode.Values[0]))
                    {
                        sameKeyPrevValueGroup.Children.AddRange(valueGroup.Children);
                        children.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        
        return node;
    }

    public static bool ThemeVariantNodeEquals(AstGroupTransformationContext context, IXamlAstValueNode left, IXamlAstValueNode right)
    {
        if (left is XamlConstantNode leftConst
            && right is XamlConstantNode rightConst)
        {
            return leftConst.Constant == rightConst.Constant;
        }
        if (left is XamlStaticExtensionNode leftStaticExt
            && right is XamlStaticExtensionNode rightStaticExt)
        {
            return leftStaticExt.Type.GetClrType().GetFullName() == rightStaticExt.Type.GetClrType().GetFullName()
                   && leftStaticExt.Member == rightStaticExt.Member;
        }
        if (left is XamlAstNewClrObjectNode leftClrObjectNode
            && right is XamlAstNewClrObjectNode rightClrObjectNode)
        {
            var themeVariant = context.GetAvaloniaTypes().ThemeVariant;
            return leftClrObjectNode.Type.GetClrType() == themeVariant
                   && leftClrObjectNode.Type == rightClrObjectNode.Type
                   && leftClrObjectNode.Constructor == rightClrObjectNode.Constructor
                   && ThemeVariantNodeEquals(context, leftClrObjectNode.Arguments.Single(),
                       leftClrObjectNode.Arguments.Single());
        }

        return false;
    }
}
