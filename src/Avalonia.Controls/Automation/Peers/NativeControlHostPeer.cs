using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers;

internal class NativeControlHostPeer : ControlAutomationPeer
{
    public NativeControlHostPeer(NativeControlHost owner)
        : base(owner)
    {
        owner.NativeControlHandleChanged += OnNativeControlHandleChanged;
    }

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        if (Owner is NativeControlHost host && host.NativeControlHandle != null)
            return [new InteropAutomationPeer(host.NativeControlHandle)];
        return null;
    }

    private void OnNativeControlHandleChanged(object? sender, EventArgs e) => InvalidateChildren();
}
