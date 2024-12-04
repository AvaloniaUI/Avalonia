using System.Collections.Generic;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition;

public sealed class CompositionCustomVisual : CompositionContainerVisual
{
    private static readonly ThreadSafeObjectPool<List<object>> s_messageListPool = new(); 
    private List<object>? _messages;

    internal CompositionCustomVisual(Compositor compositor, CompositionCustomVisualHandler handler)
        : base(compositor, new ServerCompositionCustomVisual(compositor.Server, handler))
    {

    }

    public void SendHandlerMessage(object message)
    {
        if (_messages == null)
        {
            _messages = s_messageListPool.Get();
            Compositor.RequestCompositionUpdate(OnCompositionUpdate);
        }
        _messages.Add(message);
    }

    private void OnCompositionUpdate()
    {
        if(_messages == null)
            return;
        
        var messages = _messages;
        _messages = null;
        Compositor.PostServerJob(()=>
        {
            ((ServerCompositionCustomVisual)Server).DispatchMessages(messages);
            messages.Clear();
            s_messageListPool.ReturnAndSetNull(ref messages);
        });
    }
}
