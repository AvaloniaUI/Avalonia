// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Input
{
    public interface ICloseable
    {
        event EventHandler Closed;
    }
}
