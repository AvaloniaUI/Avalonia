// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class VectorEventArgs : RoutedEventArgs
    {
        public Vector Vector { get; set; }
    }
}
