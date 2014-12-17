// -----------------------------------------------------------------------
// <copyright file="ImageTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Controls
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Media.Imaging;

    [TestClass]
    public class ImageTests : TestBase
    {
        private Bitmap bitmap;

        public ImageTests()
            : base(@"Controls\Image")
        {
            this.bitmap = new Bitmap(Path.Combine(this.OutputPath, "test.png"));
        }

        [TestMethod]
        public void Image_Stretch_None()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Red,
                    Content = new Image
                    {
                        Source = this.bitmap,
                        Stretch = Stretch.None,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Image_Stretch_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Red,
                    Content = new Image
                    {
                        Source = this.bitmap,
                        Stretch = Stretch.Fill,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Image_Stretch_Uniform()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Red,
                    Content = new Image
                    {
                        Source = this.bitmap,
                        Stretch = Stretch.Uniform,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Image_Stretch_UniformToFill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(20, 8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Red,
                    Content = new Image
                    {
                        Source = this.bitmap,
                        Stretch = Stretch.UniformToFill,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
