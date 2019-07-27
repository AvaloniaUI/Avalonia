// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class DevToolsViewModel : ViewModelBase
    {
        private IDevToolViewModel _selectedTool;
        private string _focusedControl;
        private string _pointerOverElement;

        public DevToolsViewModel(IControl root)
        {
            Tools = new ObservableCollection<IDevToolViewModel>
            {
                new TreePageViewModel(LogicalTreeNode.Create(root), "Logical Tree"),
                new TreePageViewModel(VisualTreeNode.Create(root), "Visual Tree"),
                new EventsViewModel(root)
            };

            SelectedTool = Tools.First();

            UpdateFocusedControl();

            KeyboardDevice.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
                {
                    UpdateFocusedControl();
                }
            };

            root.GetObservable(TopLevel.PointerOverElementProperty)
                .Subscribe(x => PointerOverElement = x?.GetType().Name);
        }

        public IDevToolViewModel SelectedTool
        {
            get => _selectedTool;
            set => RaiseAndSetIfChanged(ref _selectedTool, value);
        }

        public ObservableCollection<IDevToolViewModel> Tools { get; }

        public string FocusedControl
        {
            get => _focusedControl;
            private set => RaiseAndSetIfChanged(ref _focusedControl, value);
        }

        public string PointerOverElement
        {
            get => _pointerOverElement;
            private set => RaiseAndSetIfChanged(ref _pointerOverElement, value);
        }

        public void SelectControl(IControl control)
        {
            if (SelectedTool is TreePageViewModel tree)
            {
                tree.SelectControl(control);
            }
        }

        private void UpdateFocusedControl()
        {
            FocusedControl = KeyboardDevice.Instance.FocusedElement?.GetType().Name;
        }
    }
}
