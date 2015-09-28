using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var windowImpl = new Mock<IWindowImpl>();
                PerspexLocator.CurrentMutable
                    .Bind<IWindowImpl>().ToConstant(windowImpl.Object);

                var gesture1 = new KeyGesture {Key = Key.A, Modifiers = InputModifiers.Control};
                var gesture2 = new KeyGesture {Key = Key.B, Modifiers = InputModifiers.Control};

                var tl = new Window();
                
                var button = new Button();
                HotKeyManager.SetHotKey(button, gesture1);

                //ContentPresenter's parent management is broken for now, so I'm setting parent property directly
                button.SetValue(Control.ParentProperty, tl);
                Assert.Equal(gesture1, tl.KeyBindings[0].Gesture);

                HotKeyManager.SetHotKey(button, gesture2);
                Assert.Equal(gesture2, tl.KeyBindings[0].Gesture);

                button.SetValue(Control.ParentProperty, null);

                Assert.Empty(tl.KeyBindings);

                button.SetValue(Control.ParentProperty, tl);

                Assert.Equal(gesture2, tl.KeyBindings[0].Gesture);

                HotKeyManager.SetHotKey(button, null);
                Assert.Empty(tl.KeyBindings);

            }
        }
    }
}
