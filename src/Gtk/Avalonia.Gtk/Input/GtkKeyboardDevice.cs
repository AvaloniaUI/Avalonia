// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Input;


namespace Avalonia.Gtk
{
    public class GtkKeyboardDevice : KeyboardDevice
    {
        public new static GtkKeyboardDevice Instance { get; } = new GtkKeyboardDevice();
    }
}