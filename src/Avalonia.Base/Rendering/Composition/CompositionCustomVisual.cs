using System.Collections.Generic;
using System.Numerics;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

public sealed class CompositionCustomVisual : CompositionContainerVisual
{
    private List<object>? _messages;

    internal CompositionCustomVisual(Compositor compositor, CompositionCustomVisualHandler handler)
        : base(compositor, new ServerCompositionCustomVisual(compositor.Server, handler))
    {

    }

    public void SendHandlerMessage(object message)
    {
        (_messages ??= new()).Add(message);
        RegisterForSerialization();
    }

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        base.SerializeChangesCore(writer);
        if (_messages == null || _messages.Count == 0)
            writer.Write(0);
        else
        {
            writer.Write(_messages.Count);
            foreach (var m in _messages)
                writer.WriteObject(m);
            _messages.Clear();
        }
    }
}
