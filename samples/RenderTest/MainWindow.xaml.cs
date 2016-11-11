// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering;

namespace RenderTest
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.CreateAnimations();
            this.AttachDevTools();
            RendererMixin.DrawFpsCounter = true;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreateAnimations()
        {
            const int Count = 100;
            var panel = new WrapPanel();

            for (var i = 0; i < Count; ++i)
            {
                var element = new Panel
                {
                    Children =
                    {
                        new Ellipse
                        {
                            Width = 100,
                            Height = 100,
                            Fill = Brushes.Blue,
                        },
                        new Path
                        {
                            Data = StreamGeometry.Parse(
                                "F1 M 16.6309,18.6563C 17.1309,8.15625 29.8809,14.1563 29.8809,14.1563C 30.8809,11.1563 34.1308,11.4063 34.1308,11.4063C 33.5,12 34.6309,13.1563 34.6309,13.1563C 32.1309,13.1562 31.1309,14.9062 31.1309,14.9062C 41.1309,23.9062 32.6309,27.9063 32.6309,27.9062C 24.6309,24.9063 21.1309,22.1562 16.6309,18.6563 Z M 16.6309,19.9063C 21.6309,24.1563 25.1309,26.1562 31.6309,28.6562C 31.6309,28.6562 26.3809,39.1562 18.3809,36.1563C 18.3809,36.1563 18,38 16.3809,36.9063C 15,36 16.3809,34.9063 16.3809,34.9063C 16.3809,34.9063 10.1309,30.9062 16.6309,19.9063 Z"),
                            Fill = Brushes.Green,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            RenderTransform = new ScaleTransform(2, 2),
                        }
                    },
                    Margin = new Thickness(4),
                    RenderTransform = new ScaleTransform(),
                };

                var start = Animate.Stopwatch.Elapsed;
                var index = i;
                var degrees = Animate.Timer
                    .Select(x => (x - start).TotalSeconds)
                    .Where(x => (x % Count) >= index && (x % Count) < index + 1)
                    .Select(x => (x % 1) / 1);

                element.RenderTransform.Bind(
                    ScaleTransform.ScaleXProperty,
                    degrees,
                    BindingPriority.Animation);

                panel.Children.Add(element);
            }

            Content = panel;
        }
    }
}
