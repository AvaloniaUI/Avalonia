// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class InputElement_Focus
    {
        [Fact]
        public void Focus_Should_Set_FocusManager_Current()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();

                Assert.Same(target, target.GetFocusManager().FocusedElement);
            }
        }

        [Fact]
        public void Focus_Should_Be_Cleared_When_Control_Is_Removed_From_VisualTree()
        {
            Button target;

            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var root = new TestRoot
                {
                    Child = target = new Button()
                };

                target.Focus();
                root.Child = null;

                Assert.Null(root.GetFocusManager().FocusedElement);
                Assert.False(target.IsFocused);
            }
        }
    }
}
