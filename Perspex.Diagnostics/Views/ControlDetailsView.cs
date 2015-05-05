// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.Views
{
    using Perspex.Controls;
    using Perspex.Diagnostics.ViewModels;
    using ReactiveUI;
    using System;
    using System.Reactive.Linq;

    internal class ControlDetailsView : UserControl
    {
        private static readonly PerspexProperty<ControlDetailsViewModel> ViewModelProperty =
            PerspexProperty.Register<ControlDetailsView, ControlDetailsViewModel>("ViewModel");

        public ControlDetailsView()
        {
            this.InitializeComponent();
            this.GetObservable(DataContextProperty)
                .Subscribe(x => this.ViewModel = (ControlDetailsViewModel)x);
        }

        public ControlDetailsViewModel ViewModel
        {
            get { return this.GetValue(ViewModelProperty); }
            private set { this.SetValue(ViewModelProperty, value); }
        }

        private void InitializeComponent()
        {
            this.Content = new ScrollViewer
            {
                Content = new ItemsControl
                {
                    DataTemplates = new DataTemplates
                    {
                        new DataTemplate<PropertyDetails>(x =>
                            new StackPanel
                            {
                                Gap = 16,
                                Orientation = Orientation.Horizontal,
                                Children = new Controls
                                {
                                    new TextBlock { Text = x.Name },
                                    new TextBlock { [!TextBlock.TextProperty] = x.WhenAnyValue(v => v.Value).Select(v => v?.ToString()) },
                                    new TextBlock { Text = x.Priority },
                                },
                            }),
                    },
                    [!ItemsControl.ItemsProperty] = this.WhenAnyValue(x => x.ViewModel.Properties),
                }
            };
        }
    }
}
