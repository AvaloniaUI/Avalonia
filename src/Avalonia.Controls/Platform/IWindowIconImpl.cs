// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;

namespace Avalonia.Platform
{
    public interface IWindowIconImpl
    {
        void Save(Stream outputStream);
    }
}
