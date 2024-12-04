using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal partial class CompositorDrawingContextProxy
{

    private PooledList<PendingCommand> _commands = new();
    private bool _autoFlush;


    enum PendingCommandType
    {
        SetTransform,
        PushClip,
        PushOpacity,
        PushOpacityMask,
        PushGeometryClip,
        PushRenderOptions,
        PushEffect
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PendingCommandObjectUnion
    {
        [FieldOffset(0)] public IEffect? Effect;
        [FieldOffset(0)] public IBrush? Mask;
        [FieldOffset(0)] public IGeometryImpl? Clip;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PendingCommandDataUnion
    {
        // PushOpacity
        [FieldOffset(0)] public double Opacity;
        [FieldOffset(8)] public Rect? NullableOpacityRect;

        [FieldOffset(0)] public Matrix Transform;

        [FieldOffset(0)] public RenderOptions RenderOptions;

        // PushClip/PushOpacityMask
        [FieldOffset(0)] public bool IsRoundRect;
        [FieldOffset(4)] public RoundedRect RoundRect;
        [FieldOffset(4)] public Rect NormalRect;
    }

    struct PendingCommand
    {
        public PendingCommandType Type;
        public PendingCommandObjectUnion ObjectUnion;
        public PendingCommandDataUnion DataUnion;

    }

    public bool AutoFlush
    {
        get => _autoFlush;
        set
        {
            _autoFlush = value;
            if (value)
                Flush();
        }
    }
    
    public void SetTransform(Matrix m)
    {
        if (_autoFlush)
        {
            SetImplTransform(m);
            return;
        }
        var cmd = new PendingCommand
        {
            Type = PendingCommandType.SetTransform,
            DataUnion = { Transform = m }
        };
        if (_commands.Count > 0 && _commands[_commands.Count - 1].Type == PendingCommandType.SetTransform)
            _commands[_commands.Count - 1] = cmd;
        else
            _commands.Add(cmd);
    }


    private bool TryDiscardOrFlush(PendingCommandType type)
    {
        for (var c = _commands.Count - 1; c >= 0; c--)
        {
            if(_commands[c].Type == PendingCommandType.SetTransform)
                continue;
            if (_commands[c].Type == type)
            {
                _commands.RemoveRange(c, _commands.Count - c);
                return true;
            }
            break;
        }
        
        // We've failed to collapse PushX,SetTransform,PopX stack, so we need to execute any pending commands
        Flush();
        return false;
    }

    void AddCommand(PendingCommand command)
    {
        if(_autoFlush)
            ExecCommand(ref command);
        else
            _commands.Add(command);
    }

    void ExecCommand(ref PendingCommand cmd)
    {
        if (cmd.Type == PendingCommandType.SetTransform)
        {
            SetImplTransform(cmd.DataUnion.Transform);
            return;
        }
        
        SaveTransform();
        if (cmd.Type == PendingCommandType.PushOpacity)
            _impl.PushOpacity(cmd.DataUnion.Opacity, cmd.DataUnion.NullableOpacityRect);
        else if (cmd.Type == PendingCommandType.PushOpacityMask)
            _impl.PushOpacityMask(cmd.ObjectUnion.Mask!, cmd.DataUnion.NormalRect);
        else if (cmd.Type == PendingCommandType.PushClip)
        {
            if (cmd.DataUnion.IsRoundRect)
                _impl.PushClip(cmd.DataUnion.RoundRect);
            else
                _impl.PushClip(cmd.DataUnion.NormalRect);
        }
        else if (cmd.Type == PendingCommandType.PushGeometryClip)
            _impl.PushGeometryClip(cmd.ObjectUnion.Clip!);
        else if (cmd.Type == PendingCommandType.PushEffect)
        {
            if (_impl is IDrawingContextImplWithEffects effects)
                effects.PushEffect(cmd.ObjectUnion.Effect!);
        }
        else if (cmd.Type == PendingCommandType.PushRenderOptions)
            _impl.PushRenderOptions(cmd.DataUnion.RenderOptions);
        else
            Debug.Assert(false);
    }
    
    public void Flush()
    {
        var commands = _commands.AsSpan();
        for (var index = 0; index < commands.Length; index++) 
            ExecCommand(ref commands[index]);

        _commands.Clear();
        
    }
}
