// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Media;
using Avalonia.Styling;
using ReactiveUI;

namespace Avalonia.Diagnostics.Views
{
    internal class ControlDetailsView : UserControl
    {
        private static readonly StyledProperty<ControlDetailsViewModel> ViewModelProperty =
            AvaloniaProperty.Register<ControlDetailsView, ControlDetailsViewModel>("ViewModel");

        public ControlDetailsView()
        {
            InitializeComponent();
            this.GetObservable(DataContextProperty)
                .Subscribe(x => ViewModel = (ControlDetailsViewModel)x);
        }

        public ControlDetailsViewModel ViewModel
        {
            get { return GetValue(ViewModelProperty); }
            private set { SetValue(ViewModelProperty, value); }
        }

        private void InitializeComponent()
        {
            Func<object, IEnumerable<Control>> pt = PropertyTemplate;

            Content = new ScrollViewer
            {
                Content = new SimpleGrid
                {
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
                    [!GridRepeater.ItemsProperty] = this.WhenAnyValue(x => x.ViewModel.Properties).AsBinding(),
                }
            };
        }

        private IEnumerable<Control> PropertyTemplate(object i)
        {
            var property = (PropertyDetails)i;

            yield return new TextBlock
            {
                Text = property.Name,
                TextWrapping = TextWrapping.NoWrap,
                [!ToolTip.TipProperty] = property
                    .WhenAnyValue(x => x.Diagnostic)
                    .AsBinding(),
            };

            yield return new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                [!TextBlock.TextProperty] = property
                    .WhenAnyValue(v => v.Value)
                    .Select(v => v?.ToString())
                    .AsBinding(),
            };

            yield return new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                [!TextBlock.TextProperty] = property.WhenAnyValue(x => x.Priority).AsBinding(),
            };
        }
    }
}
