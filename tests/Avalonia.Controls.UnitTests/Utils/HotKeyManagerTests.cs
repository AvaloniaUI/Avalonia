using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Utils
{
    public class HotKeyManagerTests
    {
        [Fact]
        public void HotKeyManager_Should_Register_And_Unregister_Key_Binding()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var styler = new Mock<Styler>();

                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock())
                    .Bind<IStyler>().ToConstant(styler.Object);

                var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);
                var gesture2 = new KeyGesture(Key.B, KeyModifiers.Control);

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

        [Fact]
        public void HotKeyManager_Release_Reference_When_Control_Detached()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var styler = new Mock<Styler>();

                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock())
                    .Bind<IStyler>().ToConstant(styler.Object);

                var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);

                WeakReference reference = null;

                var tl = new Window();

                new Action(() =>
                {
                    var button = new Button();
                    reference = new WeakReference(button, true);
                    tl.Content = button;
                    tl.Template = CreateWindowTemplate();
                    tl.ApplyTemplate();
                    tl.Presenter.ApplyTemplate();
                    HotKeyManager.SetHotKey(button, gesture1);

                    // Detach the button from the logical tree, so there is no reference to it
                    tl.Content = null;
                    tl.ApplyTemplate();
                })();


                // The button should be collected since it's detached from the listbox
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.Null(reference?.Target);
            }
        }

        [Fact]
        public void HotKeyManager_Release_Reference_When_Control_In_Item_Template_Detached()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var styler = new Mock<Styler>();

                AvaloniaLocator.CurrentMutable
                    .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformMock())
                    .Bind<IStyler>().ToConstant(styler.Object);

                var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);

                var weakReferences = new List<WeakReference>();
                var tl = new Window { SizeToContent = SizeToContent.WidthAndHeight, IsVisible = true };
                var lm = tl.LayoutManager;

                var keyGestures = new AvaloniaList<KeyGesture> { gesture1 };
                var listBox = new ListBox
                {
                    Width = 100,
                    Height = 100,
                    VirtualizationMode = ItemVirtualizationMode.None,
                    // Create a button with binding to the KeyGesture in the template and add it to references list
                    ItemTemplate = new FuncDataTemplate(typeof(KeyGesture), (o, scope) =>
                    {
                        var keyGesture = o as KeyGesture;
                        var button = new Button
                        {
                            DataContext = keyGesture, [!Button.HotKeyProperty] = new Binding("")
                        };
                        weakReferences.Add(new WeakReference(button, true));
                        return button;
                    })
                };
                // Add the listbox and render it
                tl.Content = listBox;
                lm.ExecuteInitialLayoutPass();
                listBox.Items = keyGestures;
                lm.ExecuteLayoutPass();

                // Let the button detach when clearing the source items
                keyGestures.Clear();
                lm.ExecuteLayoutPass();
                
                // Add it again to double check,and render
                keyGestures.Add(gesture1);
                lm.ExecuteLayoutPass();
                
                keyGestures.Clear();
                lm.ExecuteLayoutPass();
                
                // The button should be collected since it's detached from the listbox
                GC.Collect();
                GC.WaitForPendingFinalizers();

                
                Assert.True(weakReferences.Count > 0);
                foreach (var weakReference in weakReferences)
                {
                    Assert.Null(weakReference.Target);
                }
            }
        }


        private FuncControlTemplate CreateWindowTemplate()
        {
            return new FuncControlTemplate<Window>((parent, scope) =>
            {
                return new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                }.RegisterInNameScope(scope);
            });
        }
    }
}
