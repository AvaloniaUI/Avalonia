using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.X11.Selections.DragDrop.XdndConstants;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Selections.DragDrop;

/// <summary>
/// Manages an XDND target for a given X11 window.
/// Specs: https://www.freedesktop.org/wiki/Specifications/XDND/
/// </summary>
internal sealed class X11DropTarget
{
    private readonly IDragDropDevice _dragDropDevice;
    private readonly IXdndWindow _window;
    private readonly IntPtr _display;
    private readonly X11Atoms _atoms;
    private DragDropDataTransfer? _currentDrag;

    public X11DropTarget(IDragDropDevice dragDropDevice, IXdndWindow window, IntPtr _display, X11Atoms atoms)
    {
        _dragDropDevice = dragDropDevice;
        _window = window;
        this._display = _display;
        _atoms = atoms;

        IntPtr version = XdndVersion;
        XChangeProperty(_display, _window.Handle, _atoms.XdndAware, _atoms.ATOM, 32, PropertyMode.Replace, ref version, 1);
    }

    public void OnXdndEnter(in XClientMessageEvent message)
    {
        if (_window.InputRoot is not { } inputRoot)
            return;

        // Spec: If the version number in the XdndEnter message is higher than what the target can support,
        // the target should ignore the source.
        var version = (byte)((message.ptr2 >> 24) & 0xFF);
        if (version is < MinXdndVersion or > XdndVersion)
            return;

        // If we ever receive a new XdndEnter message while a drag is in progress, it means something went wrong.
        // In this case, assume the old drag is stale.
        DisposeCurrentDrag();

        var sourceWindow = message.ptr1;
        var hasExtraFormats = (message.ptr2 & 1) == 1;

        var formats = new HashSet<IntPtr>();

        if (hasExtraFormats &&
            XGetWindowPropertyAsIntPtrArray(_display, sourceWindow, _atoms.XdndTypeList, _atoms.ATOM) is { } formatList)
        {
            foreach (var format in formatList)
            {
                if (format != 0)
                    formats.Add(format);
            }
        }
        else
        {
            if (message.ptr3 != 0)
                formats.Add(message.ptr3);
            if (message.ptr4 != 0)
                formats.Add(message.ptr4);
            if (message.ptr5 != 0)
                formats.Add(message.ptr5);
        }

        var (dataFormats, textFormats) = DataFormatHelper.ToDataFormats(formats.ToArray(), _atoms);
        var reader = new DragDropDataReader(_atoms, textFormats, dataFormats, _display, _window.Handle);
        _currentDrag = new DragDropDataTransfer(reader, dataFormats, sourceWindow, _window.Handle, inputRoot);
    }

    public void OnXdndPosition(in XClientMessageEvent message)
    {
        if (_currentDrag is not { } drag || message.ptr1 != drag.SourceWindow)
            return;

        var screenX = (ushort)((message.ptr3 >> 16) & 0xFFFF);
        var screenY = (ushort)(message.ptr3 & 0xFFFF);
        var position = _window.PointToClient(new PixelPoint(screenX, screenY));
        var requestedEffects = XdndActionHelper.ActionToEffects(message.ptr5, _atoms);
        var eventType = drag.LastPosition is null ? RawDragEventType.DragEnter : RawDragEventType.DragOver;

        drag.LastPosition = position;
        drag.LastTimestamp = message.ptr4;

        var dragEvent = new RawDragEvent(
            _dragDropDevice,
            eventType,
            drag.InputRoot,
            position,
            drag,
            requestedEffects,
            RawInputModifiers.None);

        _dragDropDevice.ProcessRawEvent(dragEvent);

        drag.ResultEffects = dragEvent.Effects;

        var resultAction = XdndActionHelper.EffectsToAction(dragEvent.Effects, _atoms);
        SendXdndMessage(_atoms.XdndStatus, drag, resultAction == 0 ? 0 : 1, 0, 0, resultAction);
    }

    public void OnXdndLeave(in XClientMessageEvent message)
    {
        if (_currentDrag is not { } drag || message.ptr1 != drag.SourceWindow)
            return;

        var dragLeave = new RawDragEvent(
            _dragDropDevice,
            RawDragEventType.DragLeave,
            drag.InputRoot,
            default,
            drag,
            DragDropEffects.None,
            RawInputModifiers.None);

        _dragDropDevice.ProcessRawEvent(dragLeave);

        DisposeCurrentDrag();
    }

    public void OnXdndDrop(in XClientMessageEvent message)
    {
        if (_currentDrag is not { } drag || message.ptr1 != drag.SourceWindow)
            return;

        var drop = new RawDragEvent(
            _dragDropDevice,
            RawDragEventType.Drop,
            drag.InputRoot,
            drag.LastPosition ?? default,
            drag,
            drag.ResultEffects,
            RawInputModifiers.None);

        _dragDropDevice.ProcessRawEvent(drop);

        drag.ResultEffects = drop.Effects;
        drag.Dropped = true;

        DisposeCurrentDrag();
    }

    private void SendXdndMessage(
        IntPtr messageType,
        DragDropDataTransfer drag,
        IntPtr ptr2,
        IntPtr ptr3,
        IntPtr ptr4,
        IntPtr ptr5)
    {
        var evt = new XEvent
        {
            ClientMessageEvent = new XClientMessageEvent
            {
                type = XEventName.ClientMessage,
                display = _display,
                window = drag.SourceWindow,
                message_type = messageType,
                format = 32,
                ptr1 = drag.TargetWindow,
                ptr2 = ptr2,
                ptr3 = ptr3,
                ptr4 = ptr4,
                ptr5 = ptr5
            }
        };

        XSendEvent(_display, drag.SourceWindow, false, (IntPtr)EventMask.NoEventMask, ref evt);
        XFlush(_display);
    }

    private void DisposeCurrentDrag()
    {
        if (_currentDrag is not { } drag)
            return;

        _currentDrag = null;

        if (drag.Dropped)
        {
            var resultAction = XdndActionHelper.EffectsToAction(drag.ResultEffects, _atoms);
            SendXdndMessage(_atoms.XdndFinished, drag, resultAction == 0 ? 0 : 1, resultAction, 0, 0);
        }

        drag.Dispose();
    }
}
