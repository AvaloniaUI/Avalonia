// -----------------------------------------------------------------------
// <copyright file="DevToolsViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Input;
    using ReactiveUI;

    internal class DevToolsViewModel : ReactiveObject
    {
        private Control root;

        private LogicalTreeViewModel logicalTree;

        private VisualTreeViewModel visualTree;

        private ObservableAsPropertyHelper<IInputElement> focusedControl;

        private ObservableAsPropertyHelper<IInputElement> pointerOverElement;

        public DevToolsViewModel()
        {
            this.WhenAnyValue(x => x.Root).Subscribe(x =>
            {
                this.LogicalTree = new LogicalTreeViewModel(this.root);
                this.VisualTree = new VisualTreeViewModel(this.root);
            });

            this.focusedControl = KeyboardDevice.Instance
                .WhenAnyValue(x => x.FocusedElement)
                .ToProperty(this, x => x.FocusedControl);

            this.pointerOverElement = this.WhenAnyValue(x => x.Root, x => x as TopLevel)
                .Select(x => x?.GetObservable(TopLevel.PointerOverElementProperty) ?? Observable.Empty<IInputElement>())
                .Switch()
                .ToProperty(this, x => x.PointerOverElement);
        }

        public Control Root
        {
            get { return this.root; }
            set { this.RaiseAndSetIfChanged(ref this.root, value); }
        }

        public LogicalTreeViewModel LogicalTree
        {
            get { return this.logicalTree; }
            private set { this.RaiseAndSetIfChanged(ref this.logicalTree, value); }
        }

        public VisualTreeViewModel VisualTree
        {
            get { return this.visualTree; }
            private set { this.RaiseAndSetIfChanged(ref this.visualTree, value); }
        }

        public IInputElement FocusedControl
        {
            get { return this.focusedControl.Value; }
        }

        public IInputElement PointerOverElement
        {
            get { return this.pointerOverElement.Value; }
        }
    }
}
