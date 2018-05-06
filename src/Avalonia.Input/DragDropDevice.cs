using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    public class DragDropDevice : IDragDropDevice
    {
        public static readonly DragDropDevice Instance = new DragDropDevice();
        
        private Interactive _lastTarget = null;
        
        private Interactive GetTarget(IInputElement root, Point local)
        {
            var target = root.InputHitTest(local)?.GetSelfAndVisualAncestors()?.OfType<Interactive>()?.FirstOrDefault();
            if (target != null && DragDrop.GetAllowDrop(target))
                return target;
            return null;
        }
        
        private DragDropEffects RaiseDragEvent(Interactive target, Point targetLocation, RoutedEvent<DragEventArgs> routedEvent, DragDropEffects operation, IDataObject data)
        {
            if (target == null)
                return DragDropEffects.None;
            var args = new DragEventArgs(routedEvent, data, target, targetLocation)
            {
                RoutedEvent = routedEvent,
                DragEffects = operation
            };
            target.RaiseEvent(args);
            return args.DragEffects;
        }
        
        private DragDropEffects DragEnter(IInputElement inputRoot, Point point, IDataObject data, DragDropEffects effects)
        {
            _lastTarget = GetTarget(inputRoot, point);
            return RaiseDragEvent(_lastTarget, _lastTarget != null ? inputRoot.TranslatePoint(point, _lastTarget) : point, DragDrop.DragEnterEvent, effects, data);
        }

        private DragDropEffects DragOver(IInputElement inputRoot, Point point, IDataObject data, DragDropEffects effects)
        {
            var target = GetTarget(inputRoot, point);

            if (target == _lastTarget)
                return RaiseDragEvent(target, _lastTarget != null ? inputRoot.TranslatePoint(point, _lastTarget) : point, DragDrop.DragOverEvent, effects, data);
            
            try
            {
                if (_lastTarget != null)
                    _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
                return RaiseDragEvent(target, _lastTarget != null ? inputRoot.TranslatePoint(point, _lastTarget) : point, DragDrop.DragEnterEvent, effects, data);
            }
            finally
            {
                _lastTarget = target;
            }            
        }

        private void DragLeave(IInputElement inputRoot)
        {
            if (_lastTarget == null)
                return;
            try
            {
                _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
            }
            finally 
            {
                _lastTarget = null;
            }
        }

        private DragDropEffects Drop(IInputElement inputRoot, Point point, IDataObject data, DragDropEffects effects)
        {
            try
            {
                return RaiseDragEvent(_lastTarget, _lastTarget != null ? inputRoot.TranslatePoint(point, _lastTarget) : point, DragDrop.DropEvent, effects, data);
            }
            finally 
            {
                _lastTarget = null;
            }
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawDragEvent margs)
                ProcessRawEvent(margs);
        }

        private void ProcessRawEvent(RawDragEvent e)
        {
            switch (e.Type)
            {
                case RawDragEventType.DragEnter:
                    e.Effects = DragEnter(e.InputRoot, e.Location, e.Data, e.Effects);
                    break;
                case RawDragEventType.DragOver:
                    e.Effects = DragOver(e.InputRoot, e.Location, e.Data, e.Effects);
                    break;
                case RawDragEventType.DragLeave:
                    DragLeave(e.InputRoot);
                    break;
                case RawDragEventType.Drop:
                    e.Effects = Drop(e.InputRoot, e.Location, e.Data, e.Effects);
                    break;
            }
        }
    }
}