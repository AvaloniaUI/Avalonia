// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    public abstract class PathSegment : AvaloniaObject
    {
        protected internal abstract void ApplyTo(StreamGeometryContext ctx);
    }
}