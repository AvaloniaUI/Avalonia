// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Collections
{
    public interface InccDebug
    {
        Delegate[] GetCollectionChangedSubscribers();
    }
}
