// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data.Core;

namespace Avalonia.Animation
{
    public interface IAnimationSetter
    {
        PropertyPath PropertyPath {get; set; }
        object Value { get; set; }
    }
}
