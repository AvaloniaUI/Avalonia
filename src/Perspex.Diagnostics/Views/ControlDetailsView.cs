// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Diagnostics.ViewModels;
using Perspex.Styling;
using ReactiveUI;

namespace Perspex.Diagnostics.Views
{
    internal class ControlDetailsView : UserControl
    {
        private static readonly PerspexProperty<ControlDetailsViewModel> s_viewModelProperty =
            PerspexProperty.Register<ControlDetailsView, ControlDetailsViewModel>("ViewModel");

        public ControlDetailsView()
        {
            InitializeComponent();
            GetObservable(DataContextProperty)
                .Subscribe(x => ViewModel = (ControlDetailsViewModel)x);
        }

        public ControlDetailsViewModel ViewModel
        {
            get { return GetValue(s_viewModelProperty); }
            private set { SetValue(s_viewModelProperty, value); }
        }

        private void InitializeComponent()
        {
            Func<object, IEnumerable<Control>> pt = PropertyTemplate;

            Content = new ScrollViewer
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
                                new Setter(MarginProperty, new Thickness(2)),
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
