// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    public class ExitEventArgs : EventArgs
    {
        public int ApplicationExitCode
        {
            get => Environment.ExitCode;
            set => Environment.ExitCode = value;
        }
    }
}
