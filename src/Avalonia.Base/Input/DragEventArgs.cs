using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class DragEventArgs : RoutedEventArgs, IKeyModifiersEventArgs
    {
        private readonly Interactive _target;
        private readonly Point _targetLocation;

        public DragDropEffects DragEffects { get; set; }

        public IDataTransfer DataTransfer { get; }

        public KeyModifiers KeyModifiers { get; }

        public Point GetPosition(Visual relativeTo)
        {
            if (relativeTo == null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            return _target.TranslatePoint(_targetLocation, relativeTo) ?? new Point(0, 0);
        }

        public DragEventArgs(
            RoutedEvent<DragEventArgs>? routedEvent,
            IDataTransfer dataTransfer,
            Interactive target,
            Point targetLocation,
            KeyModifiers keyModifiers)
            : base(routedEvent)
        {
            DataTransfer = dataTransfer;
            _target = target;
            _targetLocation = targetLocation;
            KeyModifiers = keyModifiers;
        }
    }
}
