// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific embeddable window implementation.
    /// </summary>
    public interface IEmbeddableWindowImpl : ITopLevelImpl
    {
        event Action LostFocus;
    }
}
