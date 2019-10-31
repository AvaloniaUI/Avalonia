// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TolTipTests
    {
        private MouseTestHelper _mouseHelper = new MouseTestHelper();

        [Fact]
        public void Should_Not_Open_On_Detached_Control()
        {
            //issue #3188
            var control = new Decorator()
            {
                [ToolTip.TipProperty] = "Tip",
                [ToolTip.ShowDelayProperty] = 0
            };

            Assert.False((control as IVisual).IsAttachedToVisualTree);

            //here in issue #3188 exception is raised
            _mouseHelper.Enter(control);

            Assert.False(ToolTip.GetIsOpen(control));
        }

        [Fact]
        public void Should_Open_On_Pointer_Enter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                window.Content = target;

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.True((target as IVisual).IsAttachedToVisualTree);

                _mouseHelper.Enter(target);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Open_On_Pointer_Enter_With_Delay()
        {
            Action timercallback = null;
            var delay = TimeSpan.Zero;

            var pti = Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true);

            Mock.Get(pti)
                .Setup(v => v.StartTimer(It.IsAny<DispatcherPriority>(), It.IsAny<TimeSpan>(), It.IsAny<Action>()))
                .Callback<DispatcherPriority, TimeSpan, Action>((priority, interval, tick) =>
                {
                    delay = interval;
                    timercallback = tick;
                })
                .Returns(Disposable.Empty);

            using (UnitTestApplication.Start(TestServices.StyledWindow.With(threadingInterface: pti)))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 1
                };

                window.Content = target;

                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.True((target as IVisual).IsAttachedToVisualTree);

                _mouseHelper.Enter(target);

                Assert.Equal(TimeSpan.FromMilliseconds(1), delay);
                Assert.NotNull(timercallback);
                Assert.False(ToolTip.GetIsOpen(target));

                timercallback();

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }
    }
}
