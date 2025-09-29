using System;
using Avalonia.Input.Raw;
using Avalonia.Input;
using System.Threading.Tasks;

namespace Avalonia.X11.DragNDrop
{
    /// <summary>
    /// Drop target for Drag-N-Drop between one App without X11's selections.
    /// </summary>
    internal class X11InnerDropTarget : Ix11InnerDropTarget
    {
        private readonly X11Window _window;

        private IDragDropDevice? _dragDropDevice;
        private IDataObject? _currentDrag;
        private Point? _currentLocation; 

        private Action<RawInputEventArgs>? Input => _window.Input;
        private IDragDropDevice? DragDropDevice => _dragDropDevice ??= AvaloniaLocator.Current.GetService<IDragDropDevice>();

        public X11InnerDropTarget(X11Window window)
        {
            _window = window;
        }

        public DragDropEffects HandleDragEnter(PixelPoint coords, IDataObject dataObject, DragDropEffects effects)
        {
            if (DragDropDevice == null)
                return DragDropEffects.None;

            _currentDrag = dataObject;
            var point = _window.PointToClient(coords);
            _currentLocation = point;

            var args = new RawDragEvent(
                DragDropDevice,
                RawDragEventType.DragEnter,
                _window.InputRoot,
                point,
                _currentDrag,
                effects,
                RawInputModifiers.None
            );

            Input?.Invoke(args);

            return args.Effects;
        }

        public  DragDropEffects HandleDragOver(PixelPoint coords, DragDropEffects effects)
        {
            if (DragDropDevice == null || _currentDrag == null)
                return DragDropEffects.None;

            var point = _window.PointToClient(coords);
            _currentLocation = point;

            var args = new RawDragEvent(
                DragDropDevice,
                RawDragEventType.DragOver,
                _window.InputRoot,
                point,
                _currentDrag,
                effects,
                RawInputModifiers.None
            );

            Input?.Invoke(args);

            return args.Effects;
        }

        public DragDropEffects HandleDrop(DragDropEffects effects)
        {
            if (DragDropDevice == null || _currentDrag == null)
                return DragDropEffects.None;

            var point = _currentLocation.HasValue ? _currentLocation.Value : new Point(0, 0);

            var args = new RawDragEvent(
                DragDropDevice,
                RawDragEventType.Drop,
                _window.InputRoot,
                point,
                _currentDrag,
                effects,
                RawInputModifiers.None
            );

            Input?.Invoke(args);

            _currentDrag = null;

            return args.Effects;
        }

        public async Task<DragDropEffects> HandleDragLeave(PixelPoint coords, DragDropEffects effects)
        {
            if (DragDropDevice == null || _currentDrag == null)
                return DragDropEffects.None;

            var point = _window.PointToClient(coords);
            _currentLocation = point;

            var args = new RawDragEvent(
                DragDropDevice,
                RawDragEventType.DragLeave,
                _window.InputRoot,
                point,
                _currentDrag,
                effects,
                RawInputModifiers.None
            );

            await Task.Run(() => Input?.Invoke(args)).ConfigureAwait(false);

            _currentDrag = null;

            return args.Effects;
        }

    }
}
