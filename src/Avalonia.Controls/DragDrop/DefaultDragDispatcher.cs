using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace Avalonia.Controls.DragDrop
{
    class DefaultDragDispatcher : IDragDispatcher
    {
        public static readonly DefaultDragDispatcher Instance = new DefaultDragDispatcher();

        private Interactive _lastTarget = null;
        
        private DefaultDragDispatcher()
        {   
        }

        private Interactive GetTarget(IInputElement root, Point local)
        {
            var target = root.InputHitTest(local)?.GetSelfAndVisualAncestors()?.OfType<Interactive>()?.FirstOrDefault();
            if (target != null && DragDrop.GetAcceptDrag(target))
                return target;
            return null;
        }
        
        private DragDropEffects RaiseDragEvent(Interactive target, RoutedEvent<DragEventArgs> routedEvent, DragDropEffects operation, IDragData data)
        {
            if (target == null)
                return DragDropEffects.None;
            var args = new DragEventArgs(routedEvent, data)
            {
                RoutedEvent = routedEvent,
                DragEffects = operation
            };
            target.RaiseEvent(args);
            return args.DragEffects;
        }
        
        public DragDropEffects DragEnter(IInputElement inputRoot, Point point, IDragData data, DragDropEffects effects)
        {
            _lastTarget = GetTarget(inputRoot, point);
            return RaiseDragEvent(_lastTarget, DragDrop.DragEnterEvent, effects, data);
        }

        public DragDropEffects DragOver(IInputElement inputRoot, Point point, IDragData data, DragDropEffects effects)
        {
            var target = GetTarget(inputRoot, point);

            if (target == _lastTarget)
                return RaiseDragEvent(target, DragDrop.DragOverEvent, effects, data);
            
            try
            {
                if (_lastTarget != null)
                    _lastTarget.RaiseEvent(new RoutedEventArgs(DragDrop.DragLeaveEvent));
                return RaiseDragEvent(target, DragDrop.DragEnterEvent, effects, data);
            }
            finally
            {
                _lastTarget = target;
            }            
        }

        public void DragLeave(IInputElement inputRoot)
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

        public DragDropEffects Drop(IInputElement inputRoot, Point point, IDragData data, DragDropEffects effects)
        {
            try
            {
                return RaiseDragEvent(_lastTarget, DragDrop.DropEvent, effects, data);
            }
            finally 
            {
                _lastTarget = null;
            }
        }
    }
}