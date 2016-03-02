// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Interactivity;
using Perspex.VisualTree;

namespace Perspex.Controls
{
    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        public IVisual TargetObject { get; set; }

        public Rect TargetRect { get; set; }
    }
}
