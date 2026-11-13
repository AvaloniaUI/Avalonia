using System;
using System.Threading.Tasks;
using Android.App;
using Android.Views;
using AndroidX.Core.View;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Android.Platform
{
    internal class AndroidDragDrop : Java.Lang.Object, DragStartHelper.IOnDragStartListener, View.IOnDragListener, IDisposable
    {
        private readonly TopLevelImpl _topLevel;
        private View? _view;
        private IDataTransfer? _currentDataTransfer;
        private TaskCompletionSource<DragDropEffects>? _currentDragTcs;
        private bool _isInActiveDragDrop;
        private bool _initialEventSent;
        private Point _lastposition;
        private IDragDropDevice? _dragDropDevice;

        public AndroidDragDrop(TopLevelImpl topLevel)
        {
            _topLevel = topLevel;
            _view = topLevel.View;
            _topLevel.View?.SetOnDragListener(this);
            _dragDropDevice = AvaloniaLocator.Current.GetService<IDragDropDevice>();
        }

        void IDisposable.Dispose()
        {
            _currentDragTcs?.SetResult(DragDropEffects.None);
            _currentDragTcs = null;
            _currentDataTransfer = null;
            _topLevel.View?.SetOnDragListener(null);
            _view = null;
        }

        public bool OnDrag(View? v, DragEvent? e)
        {
            RawDragEvent? dragEvent = null;
            if (e is { } ev)
            {
                switch (ev.Action)
                {
                    case DragAction.Ended:
                        if (ev.LocalState != this)
                            return false;
                        var result = ev.Result;

                        // Android currently doesn't supprt drag effects. Drag and Drop actions are treated as copying, though the drag source
                        // can allow the target to modify the source data. We set our data to read-only.
                        _currentDragTcs?.SetResult(result ? DragDropEffects.Copy : DragDropEffects.None);
                        _currentDragTcs = null;
                        break;
                    case DragAction.Entered:
                        _isInActiveDragDrop = true;
                        _initialEventSent = false;
                        _lastposition = default;
                        // We do not get position information here, so we defer sending the event to DragAction.Location
                        break;
                    case DragAction.Exited:
                        _isInActiveDragDrop = false;
                        _initialEventSent = false;

                        if (_dragDropDevice == null ||
                                _topLevel.InputRoot is null)
                            return true;

                        dragEvent = new RawDragEvent(
                            _dragDropDevice,
                            RawDragEventType.DragLeave,
                            _topLevel.InputRoot,
                            _lastposition,
                            new ClipDescriptionToDataTransferWrapper(ev.ClipDescription!),
                            DragDropEffects.Copy,
                            RawInputModifiers.None
                            );

                        _initialEventSent = true;
                        _topLevel?.Input?.Invoke(dragEvent);
                        break;
                    case DragAction.Location:
                        if (_isInActiveDragDrop)
                        {
                            if (_dragDropDevice == null ||
                                _topLevel.InputRoot is null)
                                return true;

                            _lastposition = new Point(ev.GetX(), ev.GetY());

                            dragEvent = new RawDragEvent(
                                _dragDropDevice,
                                _initialEventSent ? RawDragEventType.DragOver : RawDragEventType.DragEnter,
                                _topLevel.InputRoot,
                                _lastposition,
                                new ClipDescriptionToDataTransferWrapper(ev.ClipDescription!),
                                DragDropEffects.Copy,
                                RawInputModifiers.None
                                );

                            _initialEventSent = true;
                            _topLevel?.Input?.Invoke(dragEvent);
                        }
                        break;
                    case DragAction.Started:
                        // This event is sent when any drag operation start on the device, including from other apps. Even if the pointer isn't within
                        // the bounds of the view, the will receive this event.
                        break;
                    case DragAction.Drop:
                        _initialEventSent = false;
                        if (_isInActiveDragDrop)
                        {
                            _isInActiveDragDrop = false;
                            if (_dragDropDevice == null ||
                                _view?.Context is not Activity activity ||
                                _topLevel.InputRoot is null)
                                return true;

                            var dropPermissions =
                                activity.RequestDragAndDropPermissions(ev);

                            try
                            {
                                dragEvent = new RawDragEvent(
                                    _dragDropDevice,
                                    RawDragEventType.Drop,
                                    _topLevel.InputRoot!,
                                    _lastposition,
                                    new ClipDataToDataTransferWrapper(ev.ClipData!, _view?.Context),
                                    DragDropEffects.Copy,
                                    RawInputModifiers.None
                                    );

                                _initialEventSent = true;
                                _topLevel?.Input?.Invoke(dragEvent);
                            }
                            finally
                            {
                                dropPermissions?.Release();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return true;
        }

        public bool OnDragStart(View? v, DragStartHelper? helper)
        {
            if (_currentDataTransfer is not { } dataTransfer || _view == null)
                return false;

            _currentDataTransfer = null;

            var clipWrapper = new DataTransferToClipDataWrapper(dataTransfer);
            var clipData = clipWrapper.GetClipData().GetAwaiter().GetResult();
            if (clipData == null)
                return false;

            var flags = DragFlags.Global | DragFlags.GlobalUriRead;

            return _view.StartDragAndDrop(clipData,
                new View.DragShadowBuilder(_view),
                this,
                (int)flags);
        }

        public Task<DragDropEffects> StartDragDrop(IDataTransfer dataTransfer)
        {
            _currentDragTcs?.SetResult(DragDropEffects.None);
            _currentDragTcs = new TaskCompletionSource<DragDropEffects>();
            _currentDataTransfer = dataTransfer;

            var dragHelper = new DragStartHelper(_view, this);

            dragHelper.Attach();

            return _currentDragTcs.Task;
        }
    }
}
