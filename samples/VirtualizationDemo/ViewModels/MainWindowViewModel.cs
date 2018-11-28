// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI.Legacy;
using ReactiveUI;

namespace VirtualizationDemo.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {

        public MainWindowViewModel()
        {
            StackPanelVM = new VirtualizationViewModel();
            GridPanelVM = new VirtualizationViewModel() {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Orientation = Orientation.Horizontal };
        }
        public VirtualizationViewModel StackPanelVM{ get; set; }
        public VirtualizationViewModel GridPanelVM{ get; set; }
    }
}
