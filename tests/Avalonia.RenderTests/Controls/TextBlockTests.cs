// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class TextBlockTests : TestBase
    {
        public TextBlockTests()
            : base(@"Controls\TextBlock")
        {
        }

        [Win32Fact("Has text")]
        public async Task Wrapping_NoWrap()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new TextBlock
                {
                    FontFamily = new FontFamily("Courier New"),
                    Background = Brushes.Red,
                    FontSize = 12,
                    Foreground = Brushes.Black,
                    Text = "Neque porro quisquam est qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit",
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.NoWrap,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
