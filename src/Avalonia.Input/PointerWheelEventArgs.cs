// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerWheelEventArgs : PointerEventArgs
    {
        public Vector Delta { get; set; }

        public PointerWheelEventArgs(IInteractive source, IPointer pointer, IVisual rootVisual,
            Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, InputModifiers modifiers, Vector delta) 
            : base(InputElement.PointerWheelChangedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            Delta = delta;
        }
    }
}
