using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Platform;

namespace Avalonia.Controls.Automation.Peers;

/// <summary>
/// Represents the root of a native control automation tree hosted by a <see cref="NativeControlHost"/>.
/// </summary>
/// <remarks>
/// This peer should be special-cased in the platform backend, as it represents a native control
/// and hence none of the standard automation peer methods are applicable.
/// </remarks>
internal class InteropAutomationPeer : AutomationPeer
{
    private AutomationPeer? _parent;

    public InteropAutomationPeer(IPlatformHandle nativeControlHandle) => NativeControlHandle = nativeControlHandle;
    public IPlatformHandle NativeControlHandle { get; }

    protected override void BringIntoViewCore() => throw new NotImplementedException();
    protected override string? GetAcceleratorKeyCore() => throw new NotImplementedException();
    protected override string? GetAccessKeyCore() => throw new NotImplementedException();
    protected override AutomationControlType GetAutomationControlTypeCore() => throw new NotImplementedException();
    protected override string? GetAutomationIdCore() => throw new NotImplementedException();
    protected override Rect GetBoundingRectangleCore() => throw new NotImplementedException();
    protected override string GetClassNameCore() => throw new NotImplementedException();
    protected override AutomationPeer? GetLabeledByCore() => throw new NotImplementedException();
    protected override string? GetNameCore() => throw new NotImplementedException();
    protected override string? GetHelpTextCore() => throw new NotImplementedException();
    protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => throw new NotImplementedException();
    protected override AutomationPeer? GetParentCore() => _parent;
    protected override bool HasKeyboardFocusCore() => throw new NotImplementedException();
    protected override bool IsContentElementCore() => throw new NotImplementedException();
    protected override bool IsControlElementCore() => throw new NotImplementedException();
    protected override bool IsEnabledCore() => throw new NotImplementedException();
    protected override bool IsKeyboardFocusableCore() => throw new NotImplementedException();
    protected override void SetFocusCore() => throw new NotImplementedException();
    protected override bool ShowContextMenuCore() => throw new NotImplementedException();

    protected internal override bool TrySetParent(AutomationPeer? parent)
    {
        _parent = parent;
        return true;
    }
}
