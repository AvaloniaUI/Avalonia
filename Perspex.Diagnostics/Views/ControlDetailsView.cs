// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.Views
{
    using Perspex.Controls;
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Styling;
    using ReactiveUI;
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
            Func<object, IEnumerable<Control>> pt = this.PropertyTemplate;

            this.Content = new ScrollViewer
            {
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    Styles = new Styles
                    {
                        new Style(x => x.Is<Control>())
                        {
                            Setters = new[]
                            {
                                new Setter(Control.MarginProperty, new Thickness(2)),
                            }
                        },
                    },
                    [GridRepeater.TemplateProperty] = pt,
                    [!GridRepeater.ItemsProperty] = this.WhenAnyValue(x => x.ViewModel.Properties),
                }
            };
        }

        private IEnumerable<Control> PropertyTemplate(object i)
        {
            var property = (PropertyDetails)i;

            yield return new TextBlock
            {
                Text = property.Name,
                [!ToolTip.TipProperty] = property.WhenAnyValue(x => x.Diagnostic),
            };

            yield return new TextBlock
            {
                [!TextBlock.TextProperty] = property.WhenAnyValue(v => v.Value).Select(v => v?.ToString()),
            };

            yield return new TextBlock
            {
                [!TextBlock.TextProperty] = property.WhenAnyValue(x => x.Priority),
            };
        }
    }
}
