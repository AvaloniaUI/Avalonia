using System;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerWheelEventArgs : PointerEventArgs
    {
        public Vector Delta { get; }

        [Unstable("This constructor might be removed in 12.0. For unit testing, consider using IHeadlessWindow.MouseWheel.")]
        public PointerWheelEventArgs(object source, IPointer pointer, Visual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers, Vector delta)
            : base(InputElement.PointerWheelChangedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
