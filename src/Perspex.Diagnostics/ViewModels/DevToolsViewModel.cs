// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Input;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class DevToolsViewModel : ReactiveObject
    {
        private Control _root;

        private LogicalTreeViewModel _logicalTree;

        private VisualTreeViewModel _visualTree;

        private ObservableAsPropertyHelper<IInputElement> _focusedControl;

        private ObservableAsPropertyHelper<IInputElement> _pointerOverElement;

        public DevToolsViewModel()
        {
            this.WhenAnyValue(x => x.Root).Subscribe(x =>
            {
                LogicalTree = new LogicalTreeViewModel(_root);
                VisualTree = new VisualTreeViewModel(_root);
            });

            _focusedControl = KeyboardDevice.Instance
                .WhenAnyValue(x => x.FocusedElement)
                .ToProperty(this, x => x.FocusedControl);

            _pointerOverElement = this.WhenAnyValue(x => x.Root, x => x as TopLevel)
                .Select(x => x?.GetObservable(TopLevel.PointerOverElementProperty) ?? Observable.Empty<IInputElement>())
                .Switch()
                .ToProperty(this, x => x.PointerOverElement);
        }

        public Control Root
        {
            get { return _root; }
            set { this.RaiseAndSetIfChanged(ref _root, value); }
        }

        public LogicalTreeViewModel LogicalTree
        {
            get { return _logicalTree; }
            private set { this.RaiseAndSetIfChanged(ref _logicalTree, value); }
        }

        public VisualTreeViewModel VisualTree
        {
            get { return _visualTree; }
            private set { this.RaiseAndSetIfChanged(ref _visualTree, value); }
        }

        public IInputElement FocusedControl
        {
            get { return _focusedControl.Value; }
        }

        public IInputElement PointerOverElement
        {
            get { return _pointerOverElement.Value; }
        }
    }
}
