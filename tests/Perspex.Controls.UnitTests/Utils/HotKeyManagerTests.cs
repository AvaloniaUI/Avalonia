// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Platform;
using Perspex.Styling;
using Xunit;

namespace Perspex.Controls.UnitTests.Utils
{
    public class HotKeyManagerTests
    {
        [Fact]
        public void HotKeyManager_Should_Register_And_Unregister_Key_Binding()
        {
            using (PerspexLocator.EnterScope())
            {
                var styler = new Mock<Styler>();

                PerspexLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock())
                    .Bind<IStyler>().ToConstant(styler.Object);

                var gesture1 = new KeyGesture {Key = Key.A, Modifiers = InputModifiers.Control};
                var gesture2 = new KeyGesture {Key = Key.B, Modifiers = InputModifiers.Control};

                var tl = new Window();
                var button = new Button();
                tl.Content = button;
                tl.Template = CreateWindowTemplate();
                tl.ApplyTemplate();
                tl.Presenter.ApplyTemplate();

                HotKeyManager.SetHotKey(button, gesture1);

                Assert.Equal(gesture1, tl.KeyBindings[0].Gesture);

                HotKeyManager.SetHotKey(button, gesture2);
                Assert.Equal(gesture2, tl.KeyBindings[0].Gesture);

                tl.Content = null;
                tl.Presenter.ApplyTemplate();

                Assert.Empty(tl.KeyBindings);

                tl.Content = button;
                tl.Presenter.ApplyTemplate();

                Assert.Equal(gesture2, tl.KeyBindings[0].Gesture);

                HotKeyManager.SetHotKey(button, null);
                Assert.Empty(tl.KeyBindings);

            }
        }

        private FuncControlTemplate CreateWindowTemplate()
        {
            return new FuncControlTemplate<Window>(parent =>
            {
                return new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                };
            });
        }
    }
}
