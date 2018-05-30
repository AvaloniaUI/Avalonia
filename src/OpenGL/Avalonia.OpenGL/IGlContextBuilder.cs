// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.OpenGL
{
    public interface IGlContextBuilder
    {
        IGlContext Build(IEnumerable<object> surfaces);
    }
}