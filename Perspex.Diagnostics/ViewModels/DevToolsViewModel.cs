// -----------------------------------------------------------------------
// <copyright file="DevToolsViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using Perspex.Controls;
    using ReactiveUI;
    using Perspex.Input;

    internal class DevToolsViewModel : ReactiveObject
    {
        private Control root;

        private LogicalTreeViewModel logicalTree;

        private VisualTreeViewModel visualTree;

        private ObservableAsPropertyHelper<IInputElement> focusedControl;

        public DevToolsViewModel()
        {
            this.WhenAnyValue(x => x.Root).Subscribe(x =>
            {
                this.LogicalTree = new LogicalTreeViewModel(root);
                this.VisualTree = new VisualTreeViewModel(root);
            });

            this.focusedControl = KeyboardDevice.Instance.WhenAnyValue(x => x.FocusedElement)
                .ToProperty(this, x => x.FocusedControl);
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
    }
}
