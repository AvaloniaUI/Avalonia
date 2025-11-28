using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class TextBlockTests : TestBase
    {
        public TextBlockTests()
            : base(@"Controls\TextBlock")
        {
        }

        [Win32Fact("Has text")]
        public async Task Should_Draw_TextDecorations()
        {
            Border target = new Border
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 30,
                Background = Brushes.White,
                Child = new TextBlock
                {
                    FontFamily = TestFontFamily,                  
                    FontSize = 12,
                    Foreground = Brushes.Black,
                    Text = "Neque porro quisquam est qui dolorem",
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.NoWrap,
                    TextDecorations = new TextDecorationCollection
                    {
                        new TextDecoration
                        {
                            Location = TextDecorationLocation.Overline,
                            StrokeThickness= 1.5,
                            StrokeThicknessUnit = TextDecorationUnit.Pixel,
                            Stroke = new SolidColorBrush(Colors.Red)
                        },
                        new TextDecoration
                        {
                            Location = TextDecorationLocation.Baseline,
                            StrokeThickness= 1.5,
                            StrokeThicknessUnit = TextDecorationUnit.Pixel,
                            Stroke = new SolidColorBrush(Colors.Green)
                        },
                        new TextDecoration
                        {
                            Location = TextDecorationLocation.Underline,
                            StrokeThickness= 1.5,
                            StrokeThicknessUnit = TextDecorationUnit.Pixel,
                            Stroke = new SolidColorBrush(Colors.Blue),
                            StrokeOffset = 2,
                            StrokeOffsetUnit = TextDecorationUnit.Pixel
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
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


        [Win32Fact("Has text")]
        public async Task RestrictedHeight_VerticalAlign()
        {
            Control text(VerticalAlignment verticalAlignment, bool clip = true, bool restrictHeight = true)
            {
                return new Border()
                {
                    BorderBrush = Brushes.Blue,
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = restrictHeight ? 20 : double.NaN,
                    Margin = new Thickness(1),
                    Child = new TextBlock
                    {
                        FontFamily = new FontFamily("Courier New"),
                        Background = Brushes.Red,
                        FontSize = 24,
                        Foreground = Brushes.Black,
                        Text = "L",
                        VerticalAlignment = verticalAlignment,
                        ClipToBounds = clip
                    }
                };
            }
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 190,
                Height = 80,

                Child = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        text(VerticalAlignment.Stretch, restrictHeight: false),
                        text(VerticalAlignment.Center),
                        text(VerticalAlignment.Stretch),
                        text(VerticalAlignment.Top),
                        text(VerticalAlignment.Bottom),
                        text(VerticalAlignment.Center, clip:false),
                        text(VerticalAlignment.Stretch, clip:false),
                        text(VerticalAlignment.Top, clip:false),
                        text(VerticalAlignment.Bottom, clip:false),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Win32Fact("Has text")]
        public async Task Should_Draw_Run_With_Background()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Child = new TextBlock
                {
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = 12,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.NoWrap,
                    Inlines = new InlineCollection
                    {
                        new Run
                        {
                            Text = "Neque porro quisquam",
                            Background = Brushes.Red
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }


        [InlineData(150, 200, TextWrapping.NoWrap)]
        [InlineData(44, 200, TextWrapping.NoWrap)]
        [InlineData(44, 400, TextWrapping.Wrap)]
        [Win32Theory("Has text")]
        public async Task Should_Measure_Arrange_TextBlock(double width, double height, TextWrapping textWrapping)
        {
            var text = "Hello World";

            var target = new StackPanel { Width = 200, Height = height };

            target.Children.Add(new TextBlock 
            { 
                Text = text, 
                Background = Brushes.Red, 
                HorizontalAlignment = HorizontalAlignment.Left, 
                TextAlignment = TextAlignment.Left,
                Width = width, 
                TextWrapping = textWrapping 
            });
            target.Children.Add(new TextBlock 
            { 
                Text = text,
                Background = Brushes.Red, 
                HorizontalAlignment = HorizontalAlignment.Left, 
                TextAlignment = TextAlignment.Center, 
                Width = width, 
                TextWrapping = textWrapping 
            });
            target.Children.Add(new TextBlock 
            { 
                Text = text, 
                Background = Brushes.Red, 
                HorizontalAlignment = HorizontalAlignment.Left, 
                TextAlignment = TextAlignment.Right, 
                Width = width, 
                TextWrapping = textWrapping 
            });

            target.Children.Add(new TextBlock
            {
                Text = text,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Left,
                Width = width,
                TextWrapping = textWrapping
            });
            target.Children.Add(new TextBlock
            {
                Text = text,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Width = width,
                TextWrapping = textWrapping
            });
            target.Children.Add(new TextBlock
            {
                Text = text,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                Width = width,
                TextWrapping = textWrapping
            });

            target.Children.Add(new TextBlock
            {
                Text = text,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Left,
                Width = width,
                TextWrapping = textWrapping
            });
            target.Children.Add(new TextBlock
            {
                Text = text,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Center,
                Width = width,
                TextWrapping = textWrapping
            });
            target.Children.Add(new TextBlock
            {
                Text = text,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                Width = width,
                TextWrapping = textWrapping
            });

            var testName = $"Should_Measure_Arrange_TextBlock_{width}_{textWrapping}";

            await RenderToFile(target, testName);
            CompareImages(testName);
        }

        [Win32Fact("Has text")]
        public async Task Should_Keep_TrailingWhiteSpace()
        {
            // <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            //   <TextBlock Margin="0 10 0 0" HorizontalAlignment="Center" VerticalAlignment="Center" Background="Magenta" FontSize="44" Text="aaaa" FontFamily="Courier New"/>
            //   <TextBlock Margin="0 10 0 0" HorizontalAlignment="Center" VerticalAlignment="Center" Background="Magenta" FontSize="44" Text="a a " FontFamily="Courier New"/>
            //   <TextBlock Margin="0 10 0 0" HorizontalAlignment="Center" VerticalAlignment="Center" Background="Magenta" FontSize="44" Text="    " FontFamily="Courier New"/>
            //   <TextBlock Margin="0 10 0 0" HorizontalAlignment="Center" VerticalAlignment="Center" Background="Magenta" FontSize="44" Text="LLLL" FontFamily="Courier New"/>
            // </StackPanel>

            var target = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 300,
                Height = 300,
            };
            target.Children.Add(CreateText("aaaa"));
            target.Children.Add(CreateText("a a "));
            target.Children.Add(CreateText("    "));
            target.Children.Add(CreateText("LLLL"));

            var testName = $"Should_Keep_TrailingWhiteSpace";
            await RenderToFile(target, testName);
            CompareImages(testName);

            static TextBlock CreateText(string text) => new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Magenta,
                FontSize = 44,
                Text = text,
                FontFamily = new FontFamily("Courier New")
            };
        }

        [Win32Fact("Has text")]
        public async Task Should_Account_For_Overhang_Leading_And_Trailing()
        {
            var target = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 300,
                Height = 600,
                Background = new SolidColorBrush(Colors.White), // Required antialiasing to work for Overhang
            };

            target.Children.Add(CreateText("f"));
            target.Children.Add(CreateText("y"));
            target.Children.Add(CreateText("ff"));
            target.Children.Add(CreateText("yy"));
            target.Children.Add(CreateText("faaf"));
            target.Children.Add(CreateText("yaay"));
            target.Children.Add(CreateText("y y "));
            target.Children.Add(CreateText("f f "));

            var testName = $"Should_Account_For_Overhang_Leading_And_Trailing";
            await RenderToFile(target, testName);
            CompareImages(testName);

            const string symbolsFont = "resm:Avalonia.Skia.RenderTests.Assets?assembly=Avalonia.Skia.RenderTests#Source Serif 4 36pt";
            static TextBlock CreateText(string text) => new TextBlock
            {
                ClipToBounds = false,
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Magenta,
                FontStyle = FontStyle.Italic,
                FontSize = 44,
                Text = text,
                FontFamily = new FontFamily(symbolsFont)
            };
        }

        [Win32Fact("Has text")]
        public async Task Should_Draw_MultiLineText_WithOverHandLeadingTrailing()
        {
            var target = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 600,
                Height = 200,
                Background = new SolidColorBrush(Colors.White), // Required antialiasing to work for Overhang
            };

            target.Children.Add(CreateText("fff Why this is a\nbig text yyy\nyyy with multiple lines fff"));

            var testName = $"Should_Draw_MultiLineText_WithOverHandLeadingTrailing";
            await RenderToFile(target, testName);
            CompareImages(testName);

            const string symbolsFont = "resm:Avalonia.Skia.RenderTests.Assets?assembly=Avalonia.Skia.RenderTests#Source Serif 4 36pt";
            static TextBlock CreateText(string text) => new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Background = Brushes.Magenta,
                FontStyle = FontStyle.Italic,
                FontSize = 44,
                Text = text,
                FontFamily = new FontFamily(symbolsFont)
            };
        }

    }
}
