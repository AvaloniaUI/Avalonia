// -----------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using ImageMagick;
    using Perspex.Controls;
    using Perspex.Media.Imaging;
    using Xunit;

    public class TestBase
    {
        static TestBase()
        {
            Direct2D1Platform.Initialize();
        }

        public TestBase(string outputPath)
        {
            string testFiles = Path.GetFullPath(@"..\..\..\..\TestFiles\Direct2D1");
            this.OutputPath = Path.Combine(testFiles, outputPath);
        }

        public string OutputPath
        {
            get;
            private set;
        }

        protected void RenderToFile(Control target, [CallerMemberName] string testName = "")
        {
            string path = Path.Combine(this.OutputPath, testName + ".out.png");

            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)target.Width,
                (int)target.Height);

            Size size = new Size(target.Width, target.Height);
            target.Measure(size);
            target.Arrange(new Rect(size));
            bitmap.Render(target);
            bitmap.Save(path);
        }

        protected void CompareImages([CallerMemberName] string testName = "")
        {
            string expectedPath = Path.Combine(this.OutputPath, testName + ".expected.png");
            string actualPath = Path.Combine(this.OutputPath, testName + ".out.png");
            MagickImage expected = new MagickImage(expectedPath);
            MagickImage actual = new MagickImage(actualPath);
            MagickErrorInfo error = expected.Compare(actual);

            if (error.NormalizedMaximumError > 0.1)
            {
                if (error.NormalizedMaximumError > 0.15)
                {
                    Assert.True(false, "NormalizedMaximumError = " + error.NormalizedMaximumError);
                }
                else
                {
                    Assert.True(false,  "Close but no cigar. NormalizedMaximumError = " + error.NormalizedMaximumError);
                }
            }
        }
    }
}
