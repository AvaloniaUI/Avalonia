using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal sealed class AvaloniaXamlIlConditionalNode : XamlAstNode,
    IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>, IXamlAstManipulationNode
{
    private readonly AvaloniaXamlIlConditionalDefaultNode? _defaultValue;
    private readonly IReadOnlyList<AvaloniaXamlIlConditionalBranchNode> _branches;

    public AvaloniaXamlIlConditionalNode(
        AvaloniaXamlIlConditionalDefaultNode defaultValue,
        IReadOnlyList<AvaloniaXamlIlConditionalBranchNode> values,
        IXamlLineInfo info) : base(info)
    {
        _defaultValue = defaultValue;
        _branches = values;
    }

    public override void VisitChildren(IXamlAstVisitor visitor)
    {
        _defaultValue?.VisitChildren(visitor);
        foreach (var branch in _branches)
        {
            branch.VisitChildren(visitor);
        }
    }

    public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
        IXamlILEmitter codeGen)
    {
        var ret = codeGen.DefineLabel();

        foreach (var platform in _branches)
        {
            var next = codeGen.DefineLabel();
            platform.EmitCondition(context, codeGen);
            codeGen.Brfalse(next);
            platform.EmitBody(context, codeGen);
            codeGen.Br(ret);
            codeGen.MarkLabel(next);
        }

        if (_defaultValue is not null)
        {
            codeGen.Emit(OpCodes.Nop);
            _defaultValue.EmitBody(context, codeGen);
        }
        else
        {
            codeGen.Pop();
        }

        codeGen.MarkLabel(ret);

        return XamlILNodeEmitResult.Void(1);
    }
}

internal abstract class AvaloniaXamlIlConditionalBranchNode : XamlAstNode, IXamlAstManipulationNode
{
    protected AvaloniaXamlIlConditionalBranchNode(IXamlLineInfo lineInfo) : base(lineInfo)
    {
    }

    public abstract void EmitCondition(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen);

    public abstract void EmitBody(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen);
}

internal abstract class AvaloniaXamlIlConditionalDefaultNode : XamlAstNode, IXamlAstManipulationNode
{
    protected AvaloniaXamlIlConditionalDefaultNode(IXamlLineInfo lineInfo) : base(lineInfo)
    {
    }

    public abstract void EmitBody(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen);
}
