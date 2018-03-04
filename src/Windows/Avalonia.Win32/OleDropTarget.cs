using Avalonia.Controls.DragDrop;
using Avalonia.Controls.DragDrop.Raw;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using IDataObject = Avalonia.Controls.DragDrop.IDataObject;
using IOleDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Avalonia.Win32
{
    class OleDropTarget : IDropTarget
    {
        private readonly IInputElement _target;
        private readonly ITopLevelImpl _tl;
        private readonly IDragDevice _dragDevice;
        
        private IDataObject _currentDrag = null;

        public OleDropTarget(ITopLevelImpl tl, IInputElement target)
        {
            _dragDevice = AvaloniaLocator.Current.GetService<IDragDevice>();
            _tl = tl;
            _target = target;
        }

        static DropEffect ConvertDropEffect(DragDropEffects operation)
        {
            DropEffect result = DropEffect.None;
            if (operation.HasFlag(DragDropEffects.Copy))
                result |= DropEffect.Copy;
            if (operation.HasFlag(DragDropEffects.Move))
                result |= DropEffect.Move;
            if (operation.HasFlag(DragDropEffects.Link))
                result |= DropEffect.Link;
            return result;
        }

        static DragDropEffects ConvertDropEffect(DropEffect effect)
        {
            DragDropEffects result = DragDropEffects.None;
            if (effect.HasFlag(DropEffect.Copy))
                result |= DragDropEffects.Copy;
            if (effect.HasFlag(DropEffect.Move))
                result |= DragDropEffects.Move;
            if (effect.HasFlag(DropEffect.Link))
                result |= DragDropEffects.Link;
            return result;
        }
        
        UnmanagedMethods.HRESULT IDropTarget.DragEnter(IOleDataObject pDataObj, int grfKeyState, long pt, ref DropEffect pdwEffect)
        {
            var dispatch = _tl?.Input;
            if (dispatch == null)
            {
                pdwEffect = DropEffect.None;
                return UnmanagedMethods.HRESULT.S_OK;
            }
            
            _currentDrag = new OleDataObject(pDataObj);
            var args = new RawDragEvent(
                _dragDevice,
                RawDragEventType.DragEnter, 
                _target, 
                GetDragLocation(pt), 
                _currentDrag, 
                ConvertDropEffect(pdwEffect)
            );
            dispatch(args);
            pdwEffect = ConvertDropEffect(args.Effects);
            
            return UnmanagedMethods.HRESULT.S_OK;
        }

        UnmanagedMethods.HRESULT IDropTarget.DragOver(int grfKeyState, long pt, ref DropEffect pdwEffect)
        {
            var dispatch = _tl?.Input;
            if (dispatch == null)
            {
                pdwEffect = DropEffect.None;
                return UnmanagedMethods.HRESULT.S_OK;
            }
            
            var args = new RawDragEvent(
                _dragDevice,
                RawDragEventType.DragOver, 
                _target, 
                GetDragLocation(pt), 
                _currentDrag, 
                ConvertDropEffect(pdwEffect)
            );
            dispatch(args);
            pdwEffect = ConvertDropEffect(args.Effects);
            
            return UnmanagedMethods.HRESULT.S_OK;  
        }

        UnmanagedMethods.HRESULT IDropTarget.DragLeave()
        {
            try
            {
                _tl?.Input(new RawDragEvent(
                    _dragDevice,  
                    RawDragEventType.DragLeave, 
                    _target, 
                    default(Point), 
                    null, 
                    DragDropEffects.None
                ));
                return UnmanagedMethods.HRESULT.S_OK;
            }
            finally
            {
                _currentDrag = null;
            }
        }

        UnmanagedMethods.HRESULT IDropTarget.Drop(IOleDataObject pDataObj, int grfKeyState, long pt, ref DropEffect pdwEffect)
        {
            try
            {
                var dispatch = _tl?.Input;
                if (dispatch == null)
                {
                    pdwEffect = DropEffect.None;
                    return UnmanagedMethods.HRESULT.S_OK;
                }
            
                _currentDrag= new OleDataObject(pDataObj);
                
                var args = new RawDragEvent(
                    _dragDevice, 
                    RawDragEventType.Drop, 
                    _target, 
                    GetDragLocation(pt), 
                    _currentDrag, 
                    ConvertDropEffect(pdwEffect)
                );
                dispatch(args);
                pdwEffect = ConvertDropEffect(args.Effects);
            
                return UnmanagedMethods.HRESULT.S_OK;  
            }
            finally
            {
                _currentDrag = null;
            }
        }

        private Point GetDragLocation(long dragPoint)
        {
            int x = (int)dragPoint;
            int y = (int)(dragPoint >> 32);

            Point screenPt = new Point(x, y);
            return _target.PointToClient(screenPt);
        }
    }
}