// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if PERSPEX_CAIRO
namespace Perspex.Cairo.RenderTests.Media
#else
namespace Perspex.Direct2D1.RenderTests.Media
#endif
{
    public class LinearGradientBrushTests : TestBase
    {
        public LinearGradientBrushTests() : base(@"Media\LinearGradientBrush")
        {
        }

        [Fact]
        public void LinearGradientBrush_RedBlue_Horizontal_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0.5),
                        EndPoint = new Point(1, 0.5),
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        }
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void LinearGradientBrush_RedBlue_Vertical_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0.5, 0),
                        EndPoint = new Point(0.5, 1),
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        }
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
