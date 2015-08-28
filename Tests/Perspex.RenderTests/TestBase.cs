﻿// -----------------------------------------------------------------------
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

#if PERSPEX_CAIRO
	using Perspex.Cairo;
#else
    using Perspex.Direct2D1;
#endif

    public class TestBase
    {
        static TestBase()
        {
#if PERSPEX_CAIRO
            CairoPlatform.Initialize();
#else
            Direct2D1Platform.Initialize();
#endif
        }

        public TestBase(string outputPath)
        {
#if PERSPEX_CAIRO
            string testFiles = Path.GetFullPath(@"..\..\..\TestFiles\Cairo");
#else
            string testFiles = Path.GetFullPath(@"..\..\..\TestFiles\Direct2D1");
#endif
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
            double error = expected.Compare(actual, ErrorMetric.RootMeanSquared);

            if (error > 0.02)
            {
                Assert.True(false, actualPath + ": Error = " + error);
            }
        }
    }
}
