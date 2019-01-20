// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public interface IPointerDevice : IInputDevice
    {
        IInputElement Captured { get; }

        void Capture(IInputElement control);

        Point GetPosition(IVisual relativeTo);

        void SceneInvalidated(IInputRoot root, Rect rect);
    }
}
