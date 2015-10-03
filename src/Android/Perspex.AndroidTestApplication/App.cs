using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Perspex.Android;
using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Themes.Default;

namespace Perspex.AndroidTestApplication
{
    public class App : Application
    {
        public App()
        {
            RegisterServices();
            InitializePlatform();

            Styles = new DefaultTheme();
        }

        protected void InitializePlatform()
        {
           AndroidPlatform.Initialize();
        }

        public Controls.Window BuildSimpleTextUI()
        {
            Controls.Window window = new Controls.Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.Green,
                Content = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(10, 10, 0, 0),
                    Children = new Controls.Controls
                    {
                        new Controls.TextBlock
                        {
                                
                            Foreground = Brushes.GhostWhite,
                            Background = Brushes.BlueViolet,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Text = "Hello! This is a test of text wrapping and me actually getting something to render for the first time in life. Im and happy to say this was by no means all me. I want to thank everyone who helped me this far.",
                            FontSize = 80,
                            Margin = new Thickness(10),
                        },
                    }
                }
            };

            return window;
        }

        public Perspex.Controls.Window BuildGridWithSomeButtonsAndStuff()
        {
            Perspex.Controls.Window window = new Perspex.Controls.Window
            {
                Title = "Perspex Test Application",
                Background = Brushes.DarkTurquoise,
                Content = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 0),    // skip the status bar area on iOS
                    ColumnDefinitions = new ColumnDefinitions
                        {
                            new ColumnDefinition(1, GridUnitType.Star),
                            new ColumnDefinition(1, GridUnitType.Star),
                        },
                    RowDefinitions = new RowDefinitions
                        {
                            new RowDefinition(40, GridUnitType.Pixel),
                            new RowDefinition(80, GridUnitType.Pixel),
                            new RowDefinition(1, GridUnitType.Star),
                            new RowDefinition(1, GridUnitType.Star),
                        },
                    Children = new Perspex.Controls.Controls
                        {
                            new TextBlock
                            {
                                [Grid.RowProperty] = 0,
                                [Grid.ColumnSpanProperty] = 2,
                                Text = "Welcome to Perspex on iOS !!!",
                                Foreground = Brushes.SteelBlue,
                                Background = Brushes.White,
                                FontSize = 22,
                                Margin = new Thickness(5,7,5,5),
                                TextAlignment = Perspex.Media.TextAlignment.Center
                            },
//
							new StackPanel
                            {
                                Orientation = Perspex.Controls.Orientation.Horizontal,
                                [Grid.RowProperty] = 1,
                                [Grid.ColumnSpanProperty] = 2,
                                Background = SolidColorBrush.Parse("#000000"),
                                Children = new Perspex.Controls.Controls
                                {
                                    new Perspex.Controls.Button
                                    {
                                        Content = "Button 1",
                                        Width = 100,
                                        Margin = new Thickness(5)
                                    },

                                    new Perspex.Controls.Button
                                    {
                                        Content = "Button 2",
                                        Width = 100,
                                        Margin = new Thickness(5)
                                    },

                                    new Perspex.Controls.Button
                                    {
                                        Content = "Button 3",
                                        Width = 100,
                                        Margin = new Thickness(5)
                                    }
                                }
                            },
//
							new Image
                            {
                                [Grid.RowProperty] = 2,
                                [Grid.ColumnProperty] = 0,
                                Margin = new Thickness(20),
								//                            Source = new Bitmap("github_icon.png"),
								Opacity = 0.4
                            },
//
							new Ellipse
                            {
                                [Grid.RowProperty] = 3,
                                [Grid.ColumnProperty] = 0,
                                Margin = new Thickness(20),
                                Fill = Brushes.Blue
                            },
//
							new TextBlock
                            {
                                [Grid.RowProperty] = 2,
                                [Grid.ColumnProperty] = 1,
                                Text = "How does this look? Is this text going to wrap and then look nice within the bounds of this widget? If not I will be extremely disappointed!\n\nWill we start a new paragraph here? If not there will be hell to pay!!!!",
                                Foreground = Brushes.White,
                                Background = Brushes.Transparent,
                                FontSize = 14,
                                Margin = new Thickness(10,30,10,10),
                            },

                            new Perspex.Controls.Shapes.Path
                            {
                                Data = StreamGeometry.Parse("M 50,50 l 15,0 l 5,-15 l 5,15 l 15,0 l -10,10 l 4,15 l -15,-9 l -15,9 l 7,-15 Z"),
                                [Grid.RowProperty] = 3,
                                [Grid.ColumnProperty] = 1,
                                Margin = new Thickness(20),
                                Fill = Brushes.White,
                                Stroke = Brushes.Blue,
                                StrokeThickness = 4
                            }
                        }
                }
            };

            return window;
        }
    }
}