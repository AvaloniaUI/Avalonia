using System;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    public class DragEventArgs : RoutedEventArgs
    {
        private readonly Interactive _target;
        private readonly Point _targetLocation;

        public DragDropEffects DragEffects { get; set; }

        public IDataObject Data { get; }

        public KeyModifiers KeyModifiers { get; }

        public Point GetPosition(Visual relativeTo)
        {
            if (relativeTo == null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            return _target.TranslatePoint(_targetLocation, relativeTo) ?? new Point(0, 0);
        }

        [Unstable("This constructor might be removed in 12.0. For unit testing, consider using DragDrop.DoDragDrop or IHeadlessWindow.DragDrop.")]
        public DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDataObject data, Interactive target, Point targetLocation, KeyModifiers keyModifiers)
            : base(routedEvent)
        {
            Data = data;
            _target = target;
            _targetLocation = targetLocation;
            KeyModifiers = keyModifiers;
        }
    }
}
