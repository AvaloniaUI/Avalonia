// -----------------------------------------------------------------------
// <copyright file="WindowTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Platform;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WindowTests
    {
        [Fact]
        public void Setting_Title_Should_Set_Impl_Title()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var windowImpl = new Mock<IWindowImpl>();
                AvaloniaLocator.CurrentMutable.Bind<IWindowImpl>().ToConstant(windowImpl.Object);

                var target = new Window();

                target.Title = "Hello World";

                windowImpl.Verify(x => x.SetTitle("Hello World"));
            }
        }
    }
}
