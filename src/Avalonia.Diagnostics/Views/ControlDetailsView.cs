// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Views
{
    internal class ControlDetailsView : UserControl
    {
        private static readonly StyledProperty<ControlDetailsViewModel> ViewModelProperty =
            AvaloniaProperty.Register<ControlDetailsView, ControlDetailsViewModel>(nameof(ViewModel));

        private SimpleGrid _grid;

        public ControlDetailsView()
        {
            InitializeComponent();
            this.GetObservable(DataContextProperty)
                .Subscribe(x => ViewModel = (ControlDetailsViewModel)x);
        }

        public ControlDetailsViewModel ViewModel
        {
            get => GetValue(ViewModelProperty);
            private set
            {
                SetValue(ViewModelProperty, value);
                _grid[GridRepeater.ItemsProperty] = value?.Properties;
            }
        }

        private void InitializeComponent()
        {
            Func<object, IEnumerable<Control>> pt = PropertyTemplate;

            Content = new ScrollViewer { Content = _grid = new SimpleGrid { [GridRepeater.TemplateProperty] = pt } };
        }

        private IEnumerable<Control> PropertyTemplate(object i)
        {
            var property = (PropertyDetails)i;

            var margin = new Thickness(2);

            yield return new TextBlock
            {
                Margin = margin,
                Text = property.Name,
                TextWrapping = TextWrapping.NoWrap,
                [!ToolTip.TipProperty] = property.GetObservable<string>(nameof(property.Diagnostic)).ToBinding()
            };

            yield return new TextBlock
            {
                Margin = margin,
                TextWrapping = TextWrapping.NoWrap,
                [!TextBlock.TextProperty] = property.GetObservable<object>(nameof(property.Value))
                    .Select(v => v?.ToString())
                    .ToBinding()
            };

            yield return new TextBlock
            {
                Margin = margin,
                TextWrapping = TextWrapping.NoWrap,
                [!TextBlock.TextProperty] = property.GetObservable<string>((nameof(property.Priority))).ToBinding()
            };
        }
    }
}
