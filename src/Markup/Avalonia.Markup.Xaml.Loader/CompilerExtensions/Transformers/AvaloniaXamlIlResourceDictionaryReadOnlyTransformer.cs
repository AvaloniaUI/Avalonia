using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

/// <summary>
/// Moves the ResourceDictionary.MakeReadOnly call, added for x:Immutable, after all other manipulations of the dictionary.
/// </summary>
internal sealed class AvaloniaXamlIlResourceDictionaryReadOnlyTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlManipulationGroupNode group && group.Children.Any(c => c is ReadOnlyNode))
            ReadOnlyNode.MoveToEnd(group, true);

        return node;
    }

    public sealed class ReadOnlyNode : XamlAstNode, IXamlAstManipulationNode, IXamlAstILEmitableNode
    {
        public ReadOnlyNode(IXamlLineInfo lineInfo) : base(lineInfo)
        {
        }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            codeGen.EmitCall(context.GetAvaloniaTypes().ResourceDictionaryMakeReadOnly);
            return XamlILNodeEmitResult.Void(1);
        }

        /// <summary>
        /// Returns true, if the group or any of its nested groups has a MakeReadOnly call.
        /// </summary>
        public static bool IsReadOnly(XamlManipulationGroupNode group)
            => group.Children.Any(c => c is ReadOnlyNode || (c is XamlManipulationGroupNode nested && IsReadOnly(nested)));

        /// <summary>
        /// Removes all MakeReadOnly calls from the group and its nested groups, and adds a single one in the end, if needed.
        /// </summary>
        public static void MoveToEnd(XamlManipulationGroupNode group, bool readOnly)
        {
            RemoveAll(group);

            if (readOnly)
                group.Children.Add(new ReadOnlyNode(group));

            static void RemoveAll(XamlManipulationGroupNode group)
            {
                group.Children.RemoveAll(c => c is ReadOnlyNode);
                foreach (var nested in group.Children.OfType<XamlManipulationGroupNode>())
                    RemoveAll(nested);
            }
        }
    }
}
