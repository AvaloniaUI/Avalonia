// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Perspex.Controls;
using Perspex.UnitTests;
using Perspex.VisualTree;

namespace Perspex.Benchmarks.Styling
{
    public class ApplyStyling : IDisposable
    {
        private IDisposable _app;
        private Window _window;

        public ApplyStyling()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            TextBox textBox;

            _window = new Window
            {
                Content = textBox = new TextBox(),
            };

            _window.ApplyTemplate();
            textBox.ApplyTemplate();

            var border = (Border)textBox.GetVisualChildren().Single();

            if (border.BorderThickness != 2)
            {
                throw new Exception("Styles not applied.");
            }

            _window.Content = null;
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        [Benchmark]
        public void Add_And_Style_TextBox()
        {
            var textBox = new TextBox();
            _window.Content = textBox;
            textBox.ApplyTemplate();
        }
    }
}
