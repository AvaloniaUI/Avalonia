// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class DevToolsViewModel : ViewModelBase
    {
        private ViewModelBase _content;
        private int _selectedTab;
        private TreePageViewModel _logicalTree;
        private TreePageViewModel _visualTree;
        private string _focusedControl;
        private string _pointerOverElement;

        public DevToolsViewModel(IControl root)
        {
            _logicalTree = new TreePageViewModel(LogicalTreeNode.Create(root));
            _visualTree = new TreePageViewModel(VisualTreeNode.Create(root));

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

        public ViewModelBase Content
        {
            get { return _content; }
            private set { _content = value; RaisePropertyChanged(); }
        }

        public int SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                _selectedTab = value;

                switch (value)
                {
                    case 0:
                        Content = _logicalTree;
                        break;
                    case 1:
                        Content = _visualTree;
                        break;
                }

                RaisePropertyChanged();
            }
        }

        public string FocusedControl
        {
            get { return _focusedControl; }
            private set { _focusedControl = value; RaisePropertyChanged(); }
        }

        public string PointerOverElement
        {
            get { return _pointerOverElement; }
            private set { _pointerOverElement = value; RaisePropertyChanged(); }
        }

        public void SelectControl(IControl control)
        {
            var tree = Content as TreePageViewModel;

            if (tree != null)
            {
                tree.SelectControl(control);
            }
        }

        private void UpdateFocusedControl()
        {
            _focusedControl = KeyboardDevice.Instance.FocusedElement?.GetType().Name;
        }
    }
}
