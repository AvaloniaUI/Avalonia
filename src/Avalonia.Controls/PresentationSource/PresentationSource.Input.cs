using System;
using System.Threading;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls;

internal partial class PresentationSource
{
    /// <summary>
    /// Handles input from <see cref="ITopLevelImpl.Input"/>.
    /// </summary>
    /// <param name="e">The event args.</param>
    private void HandleInputCore(object state)
    {
        using var _ = Diagnostic.BeginLayoutInputPass();

        var e = (RawInputEventArgs)state!;
        if (e is RawPointerEventArgs pointerArgs)
        {
            var hitTestElement = RootElement.InputHitTest(pointerArgs.Position, enabledElementsOnly: false);

            pointerArgs.InputHitTestResult = (hitTestElement, FirstEnabledAncestor(hitTestElement));
        }

        _inputManager?.ProcessInput(e);
    }

    private SendOrPostCallback _handleInputCore;
    
    private void HandleInput(RawInputEventArgs e)
    {
        if (PlatformImpl != null)
        {
            Dispatcher.UIThread.Send(_handleInputCore, e);
        }
        else
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this,
                "PlatformImpl is null, couldn't handle input.");
        }
    }
    
    
    private static IInputElement? FirstEnabledAncestor(IInputElement? hitTestElement)
    {
        var candidate = hitTestElement;
        while (candidate?.IsEffectivelyEnabled == false)
        {
            candidate = (candidate as Visual)?.VisualParent as IInputElement;
        }
        return candidate;
    }
}