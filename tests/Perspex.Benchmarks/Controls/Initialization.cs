// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Perspex.Controls;
using Perspex.Layout;
using Perspex.UnitTests;
using Perspex.VisualTree;

namespace Perspex.Benchmarks.Controls
{
    public class Initialization
    {
        [Benchmark]
        public void Add_And_Style_TextBox()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window
                {
                    Content = new TextBox(),
                };

                LayoutManager.Instance.ExecuteInitialLayoutPass(window);

                if (((TextBox)window.Content).GetVisualChildren().Count() != 1)
                {
                    throw new Exception("Control not styled.");
                }
            }
        }
    }
}
