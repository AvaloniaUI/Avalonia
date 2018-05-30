// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.OpenGL
{
    public class GlContextException : Exception
    {
        public GlContextException(string message) : base(message) { }
    }
}
