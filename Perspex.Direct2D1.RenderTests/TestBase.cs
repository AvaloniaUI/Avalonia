// -----------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests
{
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Perspex.Controls;
    using Perspex.Media;

    public class TestBase
    {
        static TestBase()
        {
            Direct2D1Platform.Initialize();
        }

        public TestBase(string outputPath)
        {
            string testFiles = Path.GetFullPath(@"..\..\..\TestFiles\Direct2D1");
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
    }
}
