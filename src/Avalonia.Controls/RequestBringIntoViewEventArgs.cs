// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        public IVisual TargetObject { get; set; }

        public Rect TargetRect { get; set; }
    }
}
