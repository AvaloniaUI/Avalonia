#nullable enable

using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

/// <summary>
/// Adds a call to EnsureCapacity before adding items to a ResourceDictionary.
/// </summary>
internal sealed class AvaloniaXamlIlEnsureResourceDictionaryCapacityTransformer : IXamlAstTransformer
{
    private readonly HashSet<XamlManipulationGroupNode> _processedGroups = new();

    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlManipulationGroupNode group)
            Apply(context, group);

        return node;
    }

    public void Apply(AstTransformationContext context, XamlManipulationGroupNode group)
    {
        var info = GetResourcesInfo(group, context.GetAvaloniaTypes());

        if (info.Mode != ResourcesMode.None && info.Count >= 2)
            group.Children.Insert(0, new EnsureCapacityNode(group, info.Count, info.ResourcesGetter));
    }

    private ResourcesInfo GetResourcesInfo(IXamlAstManipulationNode node, AvaloniaXamlIlWellKnownTypes types)
        => node switch
        {
            XamlPropertyAssignmentNode propertyAssignment => GetResourcesInfo(propertyAssignment, types),
            XamlManipulationGroupNode group => GetResourcesInfo(group, types),
            _ => default
        };

    private ResourcesInfo GetResourcesInfo(XamlManipulationGroupNode node, AvaloniaXamlIlWellKnownTypes types)
    {
        if (!_processedGroups.Add(node))
            return default;

        ResourcesInfo groupInfo = default;

        foreach (var child in node.Children)
        {
            var childInfo = GetResourcesInfo(child, types);

            if (childInfo.Mode == ResourcesMode.None)
                continue;

            if (groupInfo.Mode == ResourcesMode.None)
                groupInfo = new ResourcesInfo(childInfo.Mode, childInfo.ResourcesGetter);
            else if (groupInfo.Mode != childInfo.Mode || groupInfo.ResourcesGetter != childInfo.ResourcesGetter)
                return default;

            groupInfo.Count += childInfo.Count;
        }

        return groupInfo;
    }

    private static ResourcesInfo GetResourcesInfo(XamlPropertyAssignmentNode node, AvaloniaXamlIlWellKnownTypes types)
        => node.Property.Name switch
        {
            "Content" when node.Property.DeclaringType == types.ResourceDictionary
                => new ResourcesInfo(ResourcesMode.ResourceDictionaryContent, null) { Count = 1 },
            "Resources" when types.IResourceDictionary.IsAssignableFrom(node.Property.Getter.ReturnType)
                => new ResourcesInfo(ResourcesMode.ElementResources, node.Property.Getter) { Count = 1 },
            _
                => default
        };

    private struct ResourcesInfo
    {
        public ResourcesMode Mode { get; }
        public IXamlMethod? ResourcesGetter { get; }
        public int Count { get; set; }

        public ResourcesInfo(ResourcesMode mode, IXamlMethod? resourcesGetter)
        {
            Mode = mode;
            ResourcesGetter = resourcesGetter;
        }
    }

    private enum ResourcesMode
    {
        None,
        ResourceDictionaryContent,
        ElementResources
    }

    public sealed class EnsureCapacityNode : XamlAstNode, IXamlAstManipulationNode, IXamlAstILEmitableNode
    {
        private readonly int _capacity;
        private readonly IXamlMethod? _resourcesGetter;

        public EnsureCapacityNode(IXamlLineInfo lineInfo, int capacity, IXamlMethod? resourcesGetter)
            : base(lineInfo)
        {
            _capacity = capacity;
            _resourcesGetter = resourcesGetter;
        }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            var types = context.GetAvaloniaTypes();
            var exitLabel = codeGen.DefineLabel();

            using var local = codeGen.LocalsPool.GetLocal(types.ResourceDictionary);

            if (_resourcesGetter is not null)
                codeGen.EmitCall(_resourcesGetter);

            codeGen
                // if (value is not ResourceDictionary local) goto exit;
                .Isinst(types.ResourceDictionary)
                .Stloc(local.Local)
                .Ldloc(local.Local)
                .Brfalse(exitLabel)
                // local.EnsureCapacity(local.Count + `_capacity`);
                .Ldloc(local.Local)
                .Dup()
                .EmitCall(types.ResourceDictionaryGetCount)
                .Ldc_I4(_capacity)
                .Add()
                .EmitCall(types.ResourceDictionaryEnsureCapacity)
                // exit:
                .MarkLabel(exitLabel);

            return XamlILNodeEmitResult.Void(1);
        }
    }
}
