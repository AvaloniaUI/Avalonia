// -----------------------------------------------------------------------
// <copyright file="WindowTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia.Controls.UnitTests
{
    using System.Reactive;
    using System.Reactive.Subjects;
    using Moq;
    using Avalonia.Controls.Presenters;
    using Avalonia.Controls.Templates;
    using Avalonia.Input;
    using Avalonia.Input.Raw;
    using Avalonia.Layout;
    using Avalonia.Platform;
    using Avalonia.Rendering;
    using Avalonia.Styling;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Splat;
    using Xunit;

    public class WindowTests
    {
        [Fact]
        public void Setting_Title_Should_Set_Impl_Title()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                var windowImpl = new Mock<IWindowImpl>();
                Locator.CurrentMutable.RegisterConstant(windowImpl.Object, typeof(IWindowImpl));

                var target = new Window();

                target.Title = "Hello World";

                windowImpl.Verify(x => x.SetTitle("Hello World"));
            }
        }
    }
}
