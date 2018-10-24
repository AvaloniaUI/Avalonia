// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    public class ControlDetailsView : UserControl
    {
        private static readonly StyledProperty<ControlDetailsViewModel> ViewModelProperty =
            AvaloniaProperty.Register<ControlDetailsView, ControlDetailsViewModel>(nameof(ViewModel));

        public ControlDetailsView()
        {
            InitializeComponent();

            this.GetObservable(DataContextProperty)
                .Subscribe(x => ViewModel = (ControlDetailsViewModel)x);
        }

        internal ControlDetailsViewModel ViewModel
        {
            get => GetValue(ViewModelProperty);
            private set => SetValue(ViewModelProperty, value);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class BindingHelper : AvaloniaObject
    {
        public static readonly AttachedProperty<BindingHelper> BindBackOnEventProperty
            = AvaloniaProperty.RegisterAttached<BindingHelper, Control, BindingHelper>("BindBackOnEvent");

        public static BindingHelper GetBindBackOnEvent(Control ctrl) => ctrl.GetValue(BindBackOnEventProperty);

        public static void SetBindBackOnEvent(Control ctrl, BindingHelper value) => ctrl.SetValue(BindBackOnEventProperty, value);

        static BindingHelper()
        {
            BindBackOnEventProperty.Changed.AddClassHandler<Control>((c, e) => BindBackOnEventPropertyChanged(c));
        }

        private static void BindBackOnEventPropertyChanged(Control c)
        {
            var value = GetBindBackOnEvent(c);

            if (value != null)
            {
                var evt = Observable.Merge(value.EventNames.Select(v => Observable.FromEventPattern(c, v)));
                var avp = AvaloniaPropertyRegistry.Instance.FindRegistered(c, value.PropertyName);
                c.Bind(Control.TagProperty, value.Binding);
                c.GetObservable(Control.TagProperty).Subscribe(v => c.SetValue(avp, v));
                evt.Subscribe(_ => c.Tag = c.GetValue(avp));
            }
        }

        public string PropertyName { get; set; }
        public AvaloniaList<string> EventNames { get; set; } = new AvaloniaList<string>();

        [AssignBinding]
        public Binding Binding { get; set; }
    }
}
