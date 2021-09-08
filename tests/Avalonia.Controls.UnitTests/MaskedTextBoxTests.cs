﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class MaskedTextBoxTests
    {
        [Fact]
        public void Opening_Context_Menu_Does_not_Lose_Selection()
        {
            using (Start(FocusServices))
            {
                var target1 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ContextMenu = new TestContextMenu()
                };

                var target2 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };

                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                var root = new TestRoot() { Child = sp };

                target1.SelectionStart = 0;
                target1.SelectionEnd = 3;

                target1.Focus();
                Assert.False(target2.IsFocused);
                Assert.True(target1.IsFocused);

                target2.Focus();

                Assert.Equal("123", target1.SelectedText);
            }
        }

        [Fact]
        public void Opening_Context_Flyout_Does_not_Lose_Selection()
        {
            using (Start(FocusServices))
            {
                var target1 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ContextFlyout = new MenuFlyout
                    {
                        Items = new List<MenuItem>
                        {
                            new MenuItem { Header = "Item 1" },
                            new MenuItem {Header = "Item 2" },
                            new MenuItem {Header = "Item 3" }
                        }
                    }
                };


                target1.ApplyTemplate();

                var root = new TestRoot() { Child = target1 };

                target1.SelectionStart = 0;
                target1.SelectionEnd = 3;

                target1.Focus();
                Assert.True(target1.IsFocused);

                target1.ContextFlyout.ShowAt(target1);

                Assert.Equal("123", target1.SelectedText);
            }
        }

        [Fact]
        public void DefaultBindingMode_Should_Be_TwoWay()
        {
            Assert.Equal(
                BindingMode.TwoWay,
                TextBox.TextProperty.GetMetadata(typeof(MaskedTextBox)).DefaultBindingMode);
        }

        [Fact]
        public void CaretIndex_Can_Moved_To_Position_After_The_End_Of_Text_With_Arrow_Key()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                target.CaretIndex = 3;
                RaiseKeyEvent(target, Key.Right, 0);

                Assert.Equal(4, target.CaretIndex);
            }
        }

        [Fact]
        public void Press_Ctrl_A_Select_All_Text()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(4, target.SelectionEnd);
            }
        }

        [Fact]
        public void Press_Ctrl_A_Select_All_Null_Text()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate()
                };

                RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(0, target.SelectionEnd);
            }
        }

        [Fact]
        public void Press_Ctrl_Z_Will_Not_Modify_Text()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);

                Assert.Equal("1234", target.Text);
            }
        }

        [Fact]
        public void Typing_Beginning_With_0_Should_Not_Modify_Text_When_Bound_To_Int()
        {
            using (Start())
            {
                var source = new Class1();
                var target = new MaskedTextBox
                {
                    DataContext = source,
                    Template = CreateTemplate(),
                };

                target.ApplyTemplate();
                target.Bind(TextBox.TextProperty, new Binding(nameof(Class1.Foo), BindingMode.TwoWay));

                Assert.Equal("0", target.Text);

                target.CaretIndex = 1;
                target.RaiseEvent(new TextInputEventArgs
                {
                    RoutedEvent = InputElement.TextInputEvent,
                    Text = "2",
                });

                Assert.Equal("02", target.Text);
            }
        }

        [Fact]
        public void Control_Backspace_Should_Remove_The_Word_Before_The_Caret_If_There_Is_No_Selection()
        {
            using (Start())
            {
                MaskedTextBox textBox = new MaskedTextBox
                {
                    Text = "First Second Third Fourth",
                    CaretIndex = 5
                };

                // (First| Second Third Fourth)
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" Second Third Fourth", textBox.Text);

                // ( Second |Third Fourth)
                textBox.CaretIndex = 8;
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" Third Fourth", textBox.Text);

                // ( Thi|rd Fourth)
                textBox.CaretIndex = 4;
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" rd Fourth", textBox.Text);

                // ( rd F[ou]rth)
                textBox.SelectionStart = 5;
                textBox.SelectionEnd = 7;

                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" rd Frth", textBox.Text);

                // ( |rd Frth)
                textBox.CaretIndex = 1;
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal("rd Frth", textBox.Text);
            }
        }

        [Fact]
        public void Control_Delete_Should_Remove_The_Word_After_The_Caret_If_There_Is_No_Selection()
        {
            using (Start())
            {
                var textBox = new MaskedTextBox
                {
                    Text = "First Second Third Fourth",
                    CaretIndex = 19
                };

                // (First Second Third |Fourth)
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("First Second Third ", textBox.Text);

                // (First Second |Third )
                textBox.CaretIndex = 13;
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("First Second ", textBox.Text);

                // (First Sec|ond )
                textBox.CaretIndex = 9;
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("First Sec", textBox.Text);

                // (Fi[rs]t Sec )
                textBox.SelectionStart = 2;
                textBox.SelectionEnd = 4;

                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("Fit Sec", textBox.Text);

                // (Fit Sec| )
                textBox.Text += " ";
                textBox.CaretIndex = 7;
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("Fit Sec", textBox.Text);
            }
        }

        [Fact]
        public void Setting_SelectionStart_To_SelectionEnd_Sets_CaretPosition_To_SelectionStart()
        {
            using (Start())
            {
                var textBox = new MaskedTextBox
                {
                    Text = "0123456789"
                };

                textBox.SelectionStart = 2;
                textBox.SelectionEnd = 2;
                Assert.Equal(2, textBox.CaretIndex);
            }
        }

        [Fact]
        public void Setting_Text_Updates_CaretPosition()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Text = "Initial Text",
                    CaretIndex = 11
                };

                var invoked = false;

                target.GetObservable(TextBox.TextProperty).Skip(1).Subscribe(_ =>
                {
                    // Caret index should be set before Text changed notification, as we don't want
                    // to notify with an invalid CaretIndex.
                    Assert.Equal(7, target.CaretIndex);
                    invoked = true;
                });

                target.Text = "Changed";

                Assert.True(invoked);
            }
        }

        [Fact]
        public void Press_Enter_Does_Not_Accept_Return()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = false,
                    Text = "1234"
                };

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal("1234", target.Text);
            }
        }

        [Fact]
        public void Press_Enter_Add_Default_Newline()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true
                };

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal(Environment.NewLine, target.Text);
            }
        }

        [Theory]
        [InlineData("00/00/0000", "12102000", "12/10/2000")]
        [InlineData("LLLL", "дбs", "____")]
        [InlineData("AA", "Ü1", "__")]
        public void AsciiOnly_Should_Not_Accept_Non_Ascii(string mask, string textEventArg, string expected)
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Mask = mask,
                    AsciiOnly = true
                };

                RaiseTextEvent(target, textEventArg);

                Assert.Equal(expected, target.Text);
            }
        }

        [Fact]
        public void Programmatically_Set_Text_Should_Not_Be_Removed_On_Key_Press()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Mask = "00:00:00.000",
                    Text = "12:34:56.000"
                };

                target.CaretIndex = target.Text.Length;
                RaiseKeyEvent(target, Key.Back, 0);

                Assert.Equal("12:34:56.00_", target.Text);
            }
        }

        [Fact]
        public void Invalid_Programmatically_Set_Text_Should_Be_Rejected()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Mask = "00:00:00.000",
                    Text = "12:34:560000"
                };

                Assert.Equal("__:__:__.___", target.Text);
            }
        }

        [Theory]
        [InlineData("00/00/0000", "12102000", "**/**/****")]
        [InlineData("LLLL", "дбs", "***_")]
        [InlineData("AA#00", "S2 33", "**_**")]
        public void PasswordChar_Should_Hide_User_Input(string mask, string textEventArg, string expected)
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Mask = mask,
                    PasswordChar = '*'
                };

                RaiseTextEvent(target, textEventArg);

                Assert.Equal(expected, target.Text);
            }
        }

        [Theory]
        [InlineData("00/00/0000", "12102000", "12/10/2000")]
        [InlineData("LLLL", "дбs", "дбs_")]
        [InlineData("AA#00", "S2 33", "S2_33")]
        public void Mask_Should_Work_Correctly(string mask, string textEventArg, string expected)
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Mask = mask
                };

                RaiseTextEvent(target, textEventArg);

                Assert.Equal(expected, target.Text);
            }
        }

        [Fact]
        public void Press_Enter_Add_Custom_Newline()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true,
                    NewLine = "Test"
                };

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal("Test", target.Text);
            }
        }

        [Theory]
        [InlineData(new object[] { false, TextWrapping.NoWrap, ScrollBarVisibility.Hidden })]
        [InlineData(new object[] { false, TextWrapping.Wrap, ScrollBarVisibility.Disabled })]
        [InlineData(new object[] { true, TextWrapping.NoWrap, ScrollBarVisibility.Auto })]
        [InlineData(new object[] { true, TextWrapping.Wrap, ScrollBarVisibility.Disabled })]
        public void Has_Correct_Horizontal_ScrollBar_Visibility(
            bool acceptsReturn,
            TextWrapping wrapping,
            ScrollBarVisibility expected)
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    AcceptsReturn = acceptsReturn,
                    TextWrapping = wrapping,
                };

                Assert.Equal(expected, ScrollViewer.GetHorizontalScrollBarVisibility(target));
            }
        }

        [Fact]
        public void SelectionEnd_Doesnt_Cause_Exception()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.SelectionStart = 0;
                target.SelectionEnd = 9;

                target.Text = "123";

                RaiseTextEvent(target, "456");

                Assert.True(true);
            }
        }

        [Fact]
        public void SelectionStart_Doesnt_Cause_Exception()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.SelectionStart = 8;
                target.SelectionEnd = 9;

                target.Text = "123";

                RaiseTextEvent(target, "456");

                Assert.True(true);
            }
        }

        [Fact]
        public void SelectionStartEnd_Are_Valid_AterTextChange()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.SelectionStart = 8;
                target.SelectionEnd = 9;

                target.Text = "123";

                Assert.True(target.SelectionStart <= "123".Length);
                Assert.True(target.SelectionEnd <= "123".Length);
            }
        }

        [Fact]
        public void SelectedText_Changes_OnSelectionChange()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                Assert.True(target.SelectedText == "");

                target.SelectionStart = 2;
                target.SelectionEnd = 4;

                Assert.True(target.SelectedText == "23");
            }
        }

        [Fact]
        public void SelectedText_EditsText()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };

                target.SelectedText = "AA";
                Assert.True(target.Text == "AA0123");

                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                target.SelectedText = "BB";

                Assert.True(target.Text == "ABB123");
            }
        }

        [Fact]
        public void SelectedText_CanClearText()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };
                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                target.SelectedText = "";

                Assert.True(target.Text == "03");
            }
        }

        [Fact]
        public void SelectedText_NullClearsText()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };
                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                target.SelectedText = null;

                Assert.True(target.Text == "03");
            }
        }

        [Fact]
        public void CoerceCaretIndex_Doesnt_Cause_Exception_with_malformed_line_ending()
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789\r"
                };
                target.CaretIndex = 11;

                Assert.True(true);
            }
        }

        [Theory]
        [InlineData(Key.Up)]
        [InlineData(Key.Down)]
        [InlineData(Key.Home)]
        [InlineData(Key.End)]
        public void Textbox_doesnt_crash_when_Receives_input_and_template_not_applied(Key key)
        {
            using (Start(FocusServices))
            {
                var target1 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    IsVisible = false
                };

                var root = new TestRoot { Child = target1 };

                target1.Focus();
                Assert.True(target1.IsFocused);

                RaiseKeyEvent(target1, key, KeyModifiers.None);
            }
        }

        [Fact]
        public void TextBox_GotFocus_And_LostFocus_Work_Properly()
        {
            using (Start(FocusServices))
            {
                var target1 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                var target2 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                var root = new TestRoot { Child = sp };

                var gfcount = 0;
                var lfcount = 0;

                target1.GotFocus += (s, e) => gfcount++;
                target2.LostFocus += (s, e) => lfcount++;

                target2.Focus();
                Assert.False(target1.IsFocused);
                Assert.True(target2.IsFocused);

                target1.Focus();
                Assert.False(target2.IsFocused);
                Assert.True(target1.IsFocused);

                Assert.Equal(1, gfcount);
                Assert.Equal(1, lfcount);
            }
        }

        [Fact]
        public void TextBox_CaretIndex_Persists_When_Focus_Lost()
        {
            using (Start(FocusServices))
            {
                var target1 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                var target2 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                var root = new TestRoot { Child = sp };

                target2.Focus();
                target2.CaretIndex = 2;
                Assert.False(target1.IsFocused);
                Assert.True(target2.IsFocused);

                target1.Focus();

                Assert.Equal(2, target2.CaretIndex);
            }
        }

        [Fact]
        public void TextBox_Reveal_Password_Reset_When_Lost_Focus()
        {
            using (Start(FocusServices))
            {
                var target1 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    PasswordChar = '*'
                };
                var target2 = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();

                var root = new TestRoot { Child = sp };

                target1.Focus();
                target1.RevealPassword = true;

                target2.Focus();

                Assert.False(target1.RevealPassword);
            }
        }

        [Fact]
        public void Setting_Bound_Text_To_Null_Works()
        {
            using (Start())
            {
                var source = new Class1 { Bar = "bar" };
                var target = new MaskedTextBox { DataContext = source };

                target.Bind(TextBox.TextProperty, new Binding("Bar"));

                Assert.Equal("bar", target.Text);
                source.Bar = null;
                Assert.Null(target.Text);
            }
        }

        [Theory]
        [InlineData("abc", "d", 3, 0, 0, false, "abc")]
        [InlineData("abc", "dd", 4, 3, 3, false, "abcd")]
        [InlineData("abc", "ddd", 3, 0, 2, true, "ddc")]
        [InlineData("abc", "dddd", 4, 1, 3, true, "addd")]
        [InlineData("abc", "ddddd", 5, 3, 3, true, "abcdd")]
        public void MaxLength_Works_Properly(
            string initalText,
            string textInput,
            int maxLength,
            int selectionStart,
            int selectionEnd,
            bool fromClipboard,
            string expected)
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = initalText,
                    MaxLength = maxLength,
                    SelectionStart = selectionStart,
                    SelectionEnd = selectionEnd
                };

                if (fromClipboard)
                {
                    AvaloniaLocator.CurrentMutable.Bind<IClipboard>().ToSingleton<ClipboardStub>();

                    var clipboard = AvaloniaLocator.CurrentMutable.GetService<IClipboard>();
                    clipboard.SetTextAsync(textInput).GetAwaiter().GetResult();

                    RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
                    clipboard.ClearAsync().GetAwaiter().GetResult();
                }
                else
                {
                    RaiseTextEvent(target, textInput);
                }

                Assert.Equal(expected, target.Text);
            }
        }

        [Theory]
        [InlineData(Key.X, KeyModifiers.Control)]
        [InlineData(Key.Back, KeyModifiers.None)]
        [InlineData(Key.Delete, KeyModifiers.None)]
        [InlineData(Key.Tab, KeyModifiers.None)]
        [InlineData(Key.Enter, KeyModifiers.None)]
        public void Keys_Allow_Undo(Key key, KeyModifiers modifiers)
        {
            using (Start())
            {
                var target = new MaskedTextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true
                };
                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                AvaloniaLocator.CurrentMutable
                    .Bind<Input.Platform.IClipboard>().ToSingleton<ClipboardStub>();

                RaiseKeyEvent(target, key, modifiers);
                RaiseKeyEvent(target, Key.Z, KeyModifiers.Control); // undo
                Assert.True(target.Text == "0123");
            }
        }

        private static TestServices FocusServices => TestServices.MockThreadingInterface.With(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            renderInterface: new MockPlatformRenderInterface(),
            fontManagerImpl: new MockFontManagerImpl(),
            textShaperImpl: new MockTextShaperImpl(),
            standardCursorFactory: Mock.Of<ICursorFactory>());

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>());

        private IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<MaskedTextBox>((control, scope) =>
                new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = "Text",
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.TemplatedParent,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                }.RegisterInNameScope(scope));
        }

        private void RaiseKeyEvent(MaskedTextBox textBox, Key key, KeyModifiers inputModifiers)
        {
            textBox.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private void RaiseTextEvent(MaskedTextBox textBox, string text)
        {
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        }

        private static IDisposable Start(TestServices services = null)
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            return UnitTestApplication.Start(services ?? Services);
        }

        private class Class1 : NotifyingBase
        {
            private int _foo;
            private string _bar;

            public int Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }

            public string Bar
            {
                get { return _bar; }
                set { _bar = value; RaisePropertyChanged(); }
            }
        }

        private class ClipboardStub : IClipboard // in order to get tests working that use the clipboard
        {
            private string _text;

            public Task<string> GetTextAsync() => Task.FromResult(_text);

            public Task SetTextAsync(string text)
            {
                _text = text;
                return Task.CompletedTask;
            }

            public Task ClearAsync()
            {
                _text = null;
                return Task.CompletedTask;
            }

            public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

            public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

            public Task<object> GetDataAsync(string format) => Task.FromResult((object)null);
        }

        private class TestContextMenu : ContextMenu
        {
            public TestContextMenu()
            {
                IsOpen = true;
            }
        }
    }
}

