using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace Avalonia.X11
{
    internal class X11DragSource : IPlatformDragSource
    {
        private readonly AvaloniaX11Platform _platform;

        private DragSourceWindow? _processWindow = null;
        private IntPtr _parentWindow = IntPtr.Zero;

        public X11DragSource(AvaloniaX11Platform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }
        public async Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects)
        {
            var dataTransfer = new DataObjectToDataTransferWrapper(data);
            return await DoDragDropAsync(triggerEvent, dataTransfer, allowedEffects);
        }

        public async Task<DragDropEffects> DoDragDropAsync(PointerEventArgs triggerEvent, IDataTransfer data, DragDropEffects allowedEffects)
        {
            if (triggerEvent == null)
                throw new ArgumentNullException(nameof(triggerEvent));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!data.Formats.Any())
            {
                return DragDropEffects.None;
            }

            Dispatcher.UIThread.VerifyAccess();
            triggerEvent.Pointer.Capture(null);
            var effects = _platform.Info.Atoms.ConvertDropEffect(allowedEffects);
            var parent = FindParent(triggerEvent);

            var completionSource = new TaskCompletionSource<DragDropEffects>();
            using (var sourceWindow = new DragSourceWindow(_platform, parent, data, effects))
            {
                _processWindow = sourceWindow;
                _parentWindow = parent;

                sourceWindow.Finished += (sender, result) =>
                {
                    completionSource.TrySetResult(result);
                };

                try
                {
                    if (sourceWindow.StartDrag())
                    {
                        return await completionSource.Task;
                    }
                    return DragDropEffects.None;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Drag operation failed: {ex}");
                    completionSource.TrySetResult(DragDropEffects.None);
                    return DragDropEffects.None;
                }

                finally
                {
                    if (_parentWindow != IntPtr.Zero)
                    {
                        XLib.XSetInputFocus(_platform.Display, _parentWindow, RevertTo.Parent, IntPtr.Zero);
                        XLib.XFlush(_platform.Display);

                        _parentWindow = IntPtr.Zero;
                    }

                    _processWindow = null;
                    if (sourceWindow is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    if (!completionSource.Task.IsCompleted)
                    {
                        completionSource.TrySetResult(DragDropEffects.None);
                    }
                }
            }
        }


        public bool OnDeviceEvent(ref XIDeviceEvent ev)
        {
            return _processWindow?.OnXI2DeviceEvent(ref ev) ?? false;
        }

        private nint FindParent(PointerEventArgs triggerEvent)
        {
            if (triggerEvent.Source is Visual visualSource)
            {
                var topLevel = TopLevel.GetTopLevel(visualSource);
                if (topLevel?.PlatformImpl?.Handle != null)
                {
                    return topLevel.PlatformImpl.Handle.Handle;
                }
            }

            return _platform.Info.RootWindow != IntPtr.Zero
                ? _platform.Info.RootWindow
                : _platform.Windows.Keys.FirstOrDefault();
        }
    }
}
