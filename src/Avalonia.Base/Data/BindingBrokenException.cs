// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data
{
    /// <summary>
    /// An exception returned through <see cref="BindingNotification"/> signalling that a
    /// requested binding expression could not be evaluated.
    /// </summary>
    public class BindingBrokenException : Exception
    {
    }
}
