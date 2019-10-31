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
    }
}
