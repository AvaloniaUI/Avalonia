using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.X11.Clipboard;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.DragDrop;

/// <summary>
/// Manages XDND (drag and drop) for a given X11 window.
/// Specs: https://www.freedesktop.org/wiki/Specifications/XDND/
/// </summary>
internal sealed class XdndHandler
{
    private const byte XdndVersion = 5;

    private readonly IDragDropDevice _dragDropDevice;
    private readonly IXdndWindow _window;
    private readonly IntPtr _display;
    private readonly X11Atoms _atoms;
    private DragDropDataTransfer? _currentDrag;

    public XdndHandler(IDragDropDevice dragDropDevice, IXdndWindow window, IntPtr _display, X11Atoms atoms)
    {
        _dragDropDevice = dragDropDevice;
        _window = window;
        this._display = _display;
        _atoms = atoms;

        IntPtr version = XdndVersion;
        XChangeProperty(_display, _window.Handle, _atoms.XdndAware, _atoms.ATOM, 32, PropertyMode.Replace, ref version, 1);
    }

    public void HandleXdndEnter(in XClientMessageEvent message)
    {
        if (_window.InputRoot is not { } inputRoot)
            return;

        // Spec: If the version number in the XdndEnter message is higher than what the target can support,
        // the target should ignore the source.
        var version = (byte)((message.ptr2 >> 24) & 0xFF);
        if (version > XdndVersion)
            return;

        // If we ever receive a new XdndEnter message while a drag is in progress, it means something went wrong.
        // In this case, assume the old drag is stale.
        DisposeCurrentDrag();

        var sourceWindow = message.ptr1;
        var hasExtraFormats = (message.ptr2 & 1) == 1;

        var formats = new HashSet<IntPtr>();

        if (hasExtraFormats)
        {
            if (XGetWindowPropertyAsIntPtrArray(_display, sourceWindow, _atoms.XdndTypeList, _atoms.ATOM)
                is { } formatList)
            {
                foreach (var format in formatList)
                {
                    if (format != 0)
                        formats.Add(format);
                }
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

        var (dataFormats, _) = ClipboardDataFormatHelper.ToDataFormats(formats.ToArray(), _atoms);
        var drag = new DragDropDataTransfer(_display, sourceWindow, _window.Handle, inputRoot, dataFormats, _atoms);

        _currentDrag = drag;
    }

    public void HandleXdndPosition(in XClientMessageEvent message)
    {
        if (_currentDrag is null)
            return;

        var sourceWindow = message.ptr1;
        if (sourceWindow != _currentDrag.SourceWindow)
            return;

        var screenX = (ushort)((message.ptr3 >> 16) & 0xFFFF);
        var screenY = (ushort)(message.ptr3 & 0xFFFF);
        var position = _window.PointToClient(new PixelPoint(screenX, screenY));
        var requestedEffects = ActionToEffects(message.ptr5);
        var eventType = _currentDrag.LastPosition is null ? RawDragEventType.DragEnter : RawDragEventType.DragOver;

        _currentDrag.LastPosition = position;
        _currentDrag.LastTimestamp = message.ptr4;

        var dragEnter = new RawDragEvent(
            _dragDropDevice,
            eventType,
            _currentDrag.InputRoot,
            position,
            _currentDrag,
            requestedEffects,
            RawInputModifiers.None);

        _dragDropDevice.ProcessRawEvent(dragEnter);

        var resultAction = EffectsToAction(dragEnter.Effects);

        var evt = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = 1,
                window = sourceWindow,
                message_type = _atoms.XdndStatus,
                format = 32,
                ptr1 = _window.Handle,
                ptr2 = resultAction == 0 ? 0 : 1,
                ptr3 = 0,
                ptr4 = 0,
                ptr5 = resultAction
            }
        };

        XSendEvent(_display, sourceWindow, false, (IntPtr)EventMask.NoEventMask, ref evt);
        XFlush(_display);

        _currentDrag.ReadText();
    }

    public void HandleXdndLeave(in XClientMessageEvent message)
    {
        if (_currentDrag is null)
            return;

        var sourceWindow = message.ptr1;
        if (sourceWindow != _currentDrag.SourceWindow)
            return;

        var dragLeave = new RawDragEvent(
            _dragDropDevice,
            RawDragEventType.DragLeave,
            _currentDrag.InputRoot,
            default,
            _currentDrag,
            DragDropEffects.None,
            RawInputModifiers.None);

        _dragDropDevice.ProcessRawEvent(dragLeave);

        DisposeCurrentDrag();
    }

    public void HandleXdndDrop(in XClientMessageEvent message)
    {
        if (_currentDrag is null)
            return;

        var sourceWindow = message.ptr1;
        if (sourceWindow != _currentDrag.SourceWindow)
            return;

        var drop = new RawDragEvent(
            _dragDropDevice,
            RawDragEventType.Drop,
            _currentDrag.InputRoot,
            _currentDrag.LastPosition ?? default,
            _currentDrag,
            DragDropEffects.None,
            RawInputModifiers.None);

        _dragDropDevice.ProcessRawEvent(drop);

        DisposeCurrentDrag();
    }

    private DragDropEffects ActionToEffects(IntPtr action)
    {
        if (action == _atoms.XdndActionCopy)
            return DragDropEffects.Copy;
        if (action == _atoms.XdndActionMove)
            return DragDropEffects.Move;
        if (action == _atoms.XdndActionLink)
            return DragDropEffects.Link;
        return DragDropEffects.None;
    }

    private IntPtr EffectsToAction(DragDropEffects effects)
    {
        if ((effects & DragDropEffects.Copy) != 0)
            return _atoms.XdndActionCopy;
        if ((effects & DragDropEffects.Move) != 0)
            return _atoms.XdndActionMove;
        if ((effects & DragDropEffects.Link) != 0)
            return _atoms.XdndActionLink;
        return 0;
    }

    private void DisposeCurrentDrag()
    {
        _currentDrag?.Dispose();
        _currentDrag = null;
    }
}
