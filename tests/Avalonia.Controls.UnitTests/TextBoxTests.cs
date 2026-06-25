#nullable enable

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests : ScopedTestBase
    {
        [Fact]
        public void Opening_Context_Menu_Does_not_Lose_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ContextMenu = new TestContextMenu()
                };

                var target2 = new TextBox
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
        public void TextBox_Should_Lose_Focus_When_Disabled()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate()
                };

                target.ApplyTemplate();

                var root = new TestRoot() { Child = target };

                target.Focus();
                Assert.True(target.IsFocused);
                target.IsEnabled = false;
                Assert.False(target.IsFocused);
                Assert.False(target.IsEnabled);
            }
        }

        [Fact]
        public void Opening_Context_Flyout_Does_not_Lose_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ContextFlyout = new MenuFlyout
                    {
                        Items =
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
                TextBox.TextProperty.GetMetadata(typeof(TextBox)).DefaultBindingMode);
        }

        [Fact]
        public void TextBox_Ignore_Word_Move_In_Password_Field()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    PasswordChar = '*',
                    Text = "passw0rd"
                };

                target.ApplyTemplate();
                target.Measure(Size.Infinity);
                target.CaretIndex = 8;
                RaiseKeyEvent(target, Key.Left, KeyModifiers.Control);

                Assert.Equal(7, target.CaretIndex);
            }
        }

        [Fact]
        public void CaretIndex_Can_Moved_To_Position_After_The_End_Of_Text_With_Arrow_Key()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                target.ApplyTemplate();

                target.Measure(Size.Infinity);

                target.CaretIndex = 3;
                RaiseKeyEvent(target, Key.Right, 0);

                Assert.Equal(4, target.CaretIndex);
            }
        }

        [Fact]
        public void Control_Backspace_Should_Set_Caret_Position_To_The_Start_Of_The_Deletion()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third",
                    SelectionStart = 13,
                    SelectionEnd = 13
                };

                target.CaretIndex = 10;
                target.ApplyTemplate();

                // (First Second |Third)
                RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);
                // (First |Third)

                Assert.Equal(6, target.CaretIndex);
            }
        }

        [Fact]
        public void Control_Backspace_Should_Remove_The_Double_Whitespace_If_Caret_Index_Was_At_The_End_Of_A_Word()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third",
                    SelectionStart = 12,
                    SelectionEnd = 12
                };

                target.ApplyTemplate();

                // (First Second| Third)
                RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);
                // (First| Third)

                Assert.Equal("First Third", target.Text);
            }
        }

        [Fact]
        public void Control_Backspace_Undo_Should_Return_Caret_Position()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third",
                    SelectionStart = 9,
                    SelectionEnd = 9
                };

                target.ApplyTemplate();

                // (First Second| Third)
                RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);
                // (First| Third)

                target.Undo();
                // (First Second| Third)

                Assert.Equal(9, target.CaretIndex);
            }
        }

        [Fact]
        public void Press_Ctrl_A_Select_All_Text()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(4, target.SelectionEnd);
            }
        }

        [Fact]
        public void Press_Ctrl_A_Select_All_Null_Text()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);

                Assert.Equal("1234", target.Text);
            }
        }

        [Fact]
        public void Control_Backspace_Should_Remove_The_Word_Before_The_Caret_If_There_Is_No_Selection()
        {
            using (UnitTestApplication.Start(Services))
            {
                TextBox textBox = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third Fourth",
                    SelectionStart = 5,
                    SelectionEnd = 5
                };

                textBox.ApplyTemplate();

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
            using (UnitTestApplication.Start(Services))
            {
                TextBox textBox = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third Fourth",
                    CaretIndex = 19,
                };

                textBox.ApplyTemplate();

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
            using (UnitTestApplication.Start(Services))
            {
                var textBox = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = false,
                    Text = "1234"
                };

                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal("1234", target.Text);
            }
        }

        [Fact]
        public void Press_Enter_Add_Default_Newline()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true
                };

                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal(Environment.NewLine, target.Text);
            }
        }

        [Fact]
        public void Press_Enter_Add_Custom_Newline()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true,
                    NewLine = "Test"
                };

                target.ApplyTemplate();

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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.ApplyTemplate();

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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.ApplyTemplate();

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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.ApplyTemplate();

                Assert.True(target.SelectedText == "");

                target.SelectionStart = 2;
                target.SelectionEnd = 4;

                Assert.True(target.SelectedText == "23");
            }
        }

        [Fact]
        public void SelectedText_EditsText()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };

                target.ApplyTemplate();

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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
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
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
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
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                var target2 = new TextBox
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
            using (UnitTestApplication.Start(FocusServices.With(assetLoader: new StandardAssetLoader())))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                var target2 = new TextBox
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
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    PasswordChar = '*'
                };
                var target2 = new TextBox
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
            using (UnitTestApplication.Start(Services))
            {
                var source = new Class1 { Bar = "bar" };
                var target = new TextBox { Template = CreateTemplate(), DataContext = source };

                target.ApplyTemplate();

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
        public async Task MaxLength_Works_Properly(
            string initalText,
            string textInput,
            int maxLength,
            int selectionStart,
            int selectionEnd,
            bool fromClipboard,
            string expected)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = initalText,
                    MaxLength = maxLength,
                    SelectionStart = selectionStart,
                    SelectionEnd = selectionEnd
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.Measure(Size.Infinity);

                if (fromClipboard)
                {
                    await topLevel.Clipboard!.SetTextAsync(textInput);

                    RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
                    await topLevel.Clipboard!.ClearAsync();
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
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.SelectionStart = 1;
                target.SelectionEnd = 3;

                RaiseKeyEvent(target, key, modifiers);
                RaiseKeyEvent(target, Key.Z, KeyModifiers.Control); // undo
                Assert.True(target.Text == "0123");
            }
        }

        [Fact]
        public void Setting_SelectedText_Should_Fire_Single_Text_Changed_Notification()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    SelectionStart = 1,
                    SelectionEnd = 3,
                };

                var values = new List<string?>();
                target.GetObservable(TextBox.TextProperty).Subscribe(x => values.Add(x));

                target.SelectedText = "A";

                Assert.Equal(new[] { "0123", "0A3" }, values);
            }
        }

        [Fact]
        public void Entering_Text_With_SelectedText_Should_Fire_Single_Text_Changed_Notification()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    SelectionStart = 1,
                    SelectionEnd = 3,
                };

                var values = new List<string?>();
                target.GetObservable(TextBox.TextProperty).Subscribe(x => values.Add(x));

                RaiseTextEvent(target, "A");

                Assert.Equal(new[] { "0123", "0A3" }, values);
            }
        }

        [Fact]
        public void Insert_Multiline_Text_Should_Accept_Extra_Lines_When_AcceptsReturn_Is_True()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    AcceptsReturn = true
                };

                RaiseTextEvent(target, $"123 {Environment.NewLine}456");

                Assert.Equal($"123 {Environment.NewLine}456", target.Text);
            }
        }

        [Fact]
        public void Insert_Multiline_Text_Should_Discard_Extra_Lines_When_AcceptsReturn_Is_False()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    AcceptsReturn = false
                };

                RaiseTextEvent(target, $"123 {"\r"}456");

                Assert.Equal("123 ", target.Text);

                target.Text = "";

                RaiseTextEvent(target, $"123 {"\r\n"}456");

                Assert.Equal("123 ", target.Text);
            }
        }

        [Fact]
        public async Task Should_Fullfill_MaxLines_Contraint()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABC",
                    MaxLines = 1,
                    AcceptsReturn = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                var initialHeight = target.DesiredSize.Height;

                await topLevel.Clipboard!.SetTextAsync(Environment.NewLine);

                RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
                await topLevel.Clipboard!.ClearAsync();

                RaiseTextEvent(target, Environment.NewLine);

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                Assert.Equal(initialHeight, target.DesiredSize.Height);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MaxLines_Sets_ScrollViewer_MaxHeight(int maxLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MaxLines = maxLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.NotNull(textPresenter);
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                Assert.Equal(new Thickness(0), textPresenter.Margin); // Test assumes no margin on TextPresenter

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.NotNull(scrollViewer);
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal(maxLines * target.LineHeight, scrollViewer.MaxHeight);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MaxLines_Sets_ScrollViewer_MaxHeight_With_TextPresenter_Margin(int maxLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MaxLines = maxLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.NotNull(textPresenter);
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                var textPresenterMargin = new Thickness(horizontal: 0, vertical: 3);
                textPresenter.Margin = textPresenterMargin;

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.NotNull(scrollViewer);
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal((maxLines * target.LineHeight) + textPresenterMargin.Top + textPresenterMargin.Bottom, scrollViewer.MaxHeight);
            }
        }

        [Fact]
        public void Should_Fullfill_MinLines_Contraint()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABC \n DEF \n GHI",
                    MinLines = 3,
                    AcceptsReturn = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                var initialHeight = target.DesiredSize.Height;

                target.Text = "";

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                Assert.Equal(initialHeight, target.DesiredSize.Height);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MinLines_Sets_ScrollViewer_MinHeight(int minLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MinLines = minLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.NotNull(textPresenter);
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                Assert.Equal(new Thickness(0), textPresenter.Margin); // Test assumes no margin on TextPresenter

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.NotNull(scrollViewer);
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal(minLines * target.LineHeight, scrollViewer.MinHeight);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MinLines_Sets_ScrollViewer_MinHeight_With_TextPresenter_Margin(int minLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MinLines = minLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.NotNull(textPresenter);
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                var textPresenterMargin = new Thickness(horizontal: 0, vertical: 3);
                textPresenter.Margin = textPresenterMargin;

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.NotNull(scrollViewer);
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal((minLines * target.LineHeight) + textPresenterMargin.Top + textPresenterMargin.Bottom, scrollViewer.MinHeight);
            }
        }

        [Theory]
        [InlineData(null, 1)]
        [InlineData("", 1)]
        [InlineData("Hello", 1)]
        [InlineData("Hello\r\nWorld", 2)]
        public void LineCount_Is_Correct(string? text, int lineCount)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = text,
                    AcceptsReturn = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                Assert.Equal(lineCount, target.GetLineCount());
            }
        }

        [Fact]
        public void Unmeasured_TextBox_Has_Negative_LineCount()
        {
            var b = new TextBox();
            Assert.Equal(-1, b.GetLineCount());
        }

        [Fact]
        public void LineCount_Is_Correct_After_Text_Change()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "Hello",
                    AcceptsReturn = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                Assert.Equal(1, target.GetLineCount());

                target.Text = "Hello\r\nWorld";

                Assert.Equal(2, target.GetLineCount());
            }
        }

        [Fact]
        public void Visible_LineCount_DoesNot_Affect_LineCount()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "Hello\r\nWorld\r\nHello\r\nAvalonia",
                    AcceptsReturn = true,
                    MaxLines = 2,
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                Assert.Equal(4, target.GetLineCount());
            }
        }

        [Fact]
        public void CanUndo_CanRedo_Is_False_When_Initialized()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "New Text"
                };

                tb.Measure(Size.Infinity);

                Assert.False(tb.CanUndo);
                Assert.False(tb.CanRedo);
            }
        }

        [Fact]
        public void CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                };

                tb.Measure(Size.Infinity);

                // See GH #6024 for a bit more insight on when Undo/Redo snapshots are taken:
                // - Every 'Space', but only when space is handled in OnKeyDown - Spaces in TextInput event won't work
                // - Every 7 chars in a long word
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                // NOTE: the spaces won't actually add spaces b/c they're sent only as key events and not Text events
                //       so our final text is without spaces
                Assert.Equal("ABCDEF123", tb.Text);

                Assert.True(tb.CanUndo);

                tb.Undo();

                // Undo will take us back one step
                Assert.Equal("ABCDEF", tb.Text);

                Assert.True(tb.CanRedo);

                tb.Redo();

                // Redo should restore us
                Assert.Equal("ABCDEF123", tb.Text);
            }
        }

        [Fact]
        public void Setting_UndoLimit_Clears_Undo_Redo()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                };

                tb.Measure(Size.Infinity);

                // This is all the same as the above test (CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works)
                // We do this to get the undo/redo stacks in a state where both are active
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                Assert.Equal("ABCDEF123", tb.Text);
                Assert.True(tb.CanUndo);
                tb.Undo();
                // Undo will take us back one step
                Assert.Equal("ABCDEF", tb.Text);
                Assert.True(tb.CanRedo);
                tb.Redo();
                // Redo should restore us
                Assert.Equal("ABCDEF123", tb.Text);

                // Change the undo limit, this should clear both stacks setting CanUndo and CanRedo to false
                tb.UndoLimit = 1;

                Assert.False(tb.CanUndo);
                Assert.False(tb.CanRedo);
            }
        }

        [Fact]
        public void Setting_IsUndoEnabled_To_False_Clears_Undo_Redo()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                };

                tb.Measure(Size.Infinity);

                // This is all the same as the above test (CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works)
                // We do this to get the undo/redo stacks in a state where both are active
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                Assert.Equal("ABCDEF123", tb.Text);
                Assert.True(tb.CanUndo);
                tb.Undo();
                // Undo will take us back one step
                Assert.Equal("ABCDEF", tb.Text);
                Assert.True(tb.CanRedo);
                tb.Redo();
                // Redo should restore us
                Assert.Equal("ABCDEF123", tb.Text);

                // Disable Undo/Redo, this should clear both stacks setting CanUndo and CanRedo to false
                tb.IsUndoEnabled = false;

                Assert.False(tb.CanUndo);
                Assert.False(tb.CanRedo);
            }
        }

        [Fact]
        public void Command_States_Update_When_ReadOnly_And_PasswordChar_Change()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    SelectionStart = 1,
                    SelectionEnd = 3,
                };

                tb.Measure(Size.Infinity);

                Assert.True(tb.CanCopy);
                Assert.True(tb.CanCut);
                Assert.True(tb.CanPaste);

                tb.IsReadOnly = true;

                Assert.True(tb.CanCopy);
                Assert.False(tb.CanCut);
                Assert.False(tb.CanPaste);

                tb.PasswordChar = '*';

                Assert.False(tb.CanCopy);
                Assert.False(tb.CanCut);
                Assert.False(tb.CanPaste);

                tb.IsReadOnly = false;

                Assert.False(tb.CanCopy);
                Assert.False(tb.CanCut);
                Assert.True(tb.CanPaste);

                tb.PasswordChar = default;

                Assert.True(tb.CanCopy);
                Assert.True(tb.CanCut);
                Assert.True(tb.CanPaste);
            }
        }

        [Fact]
        public void ReadOnly_Editing_Hotkeys_Do_Not_Modify_Text_Or_Undo_State()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true,
                    AcceptsTab = true,
                };

                tb.Measure(Size.Infinity);

                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");

                Assert.Equal("ABCDEF", tb.Text);

                tb.Undo();

                Assert.Equal("ABC", tb.Text);
                Assert.True(tb.CanUndo);
                Assert.True(tb.CanRedo);

                tb.IsReadOnly = true;
                tb.CaretIndex = tb.Text!.Length;
                tb.SelectionStart = 0;
                tb.SelectionEnd = tb.Text.Length;

                var originalText = tb.Text;
                var originalCaretIndex = tb.CaretIndex;
                var originalSelectionStart = tb.SelectionStart;
                var originalSelectionEnd = tb.SelectionEnd;
                var originalCanUndo = tb.CanUndo;
                var originalCanRedo = tb.CanRedo;

                var cutRaised = 0;
                var pasteRaised = 0;
                tb.CuttingToClipboard += (_, _) => cutRaised++;
                tb.PastingFromClipboard += (_, _) => pasteRaised++;

                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current!.PlatformSettings!.HotkeyConfiguration.Cut, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current.PlatformSettings.HotkeyConfiguration.Paste, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current.PlatformSettings.HotkeyConfiguration.Undo, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current.PlatformSettings.HotkeyConfiguration.Redo, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Back, KeyModifiers.None, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Back, KeyModifiers.Control, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Delete, KeyModifiers.None, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Delete, KeyModifiers.Control, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Enter, KeyModifiers.None, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Tab, KeyModifiers.None, handled: true);
                AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Space, KeyModifiers.None, handled: false);

                Assert.Equal(originalText, tb.Text);
                Assert.Equal(originalCaretIndex, tb.CaretIndex);
                Assert.Equal(originalSelectionStart, tb.SelectionStart);
                Assert.Equal(originalSelectionEnd, tb.SelectionEnd);
                Assert.Equal(originalCanUndo, tb.CanUndo);
                Assert.Equal(originalCanRedo, tb.CanRedo);
                Assert.Equal(0, cutRaised);
                Assert.Equal(0, pasteRaised);
            }
        }

        [Fact]
        public void UndoLimit_Count_Is_Respected()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    UndoLimit = 3 // Something small for this test
                };

                tb.Measure(Size.Infinity);

                // Push 3 undoable actions, we should only be able to recover 2
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                Assert.Equal("ABCDEF123", tb.Text);

                // Undo will take us back one step
                tb.Undo();
                Assert.Equal("ABCDEF", tb.Text);

                // Undo again
                tb.Undo();
                Assert.Equal("ABC", tb.Text);

                // We now should not be able to undo again
                Assert.False(tb.CanUndo);
            }
        }

        [Fact]
        public void Should_Move_Caret_To_EndOfLine()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "AB\nAB"
                };

                tb.Measure(Size.Infinity);

                RaiseKeyEvent(tb, Key.End, KeyModifiers.Shift);

                Assert.Equal(2, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(0, 4)]
        [InlineData(2, 6)]
        [InlineData(0, 6)]
        [InlineData(3, 4)]
        public void When_Selection_From_Left_To_Right_Pressing_Right_Should_Remove_Selection_Moving_Caret_To_End_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Right, KeyModifiers.None);

                Assert.Equal(selectionEnd, tb.SelectionStart);
                Assert.Equal(selectionEnd, tb.SelectionEnd);
                Assert.Equal(selectionEnd, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(0, 4)]
        [InlineData(2, 6)]
        [InlineData(0, 6)]
        [InlineData(3, 4)]
        public void When_Selection_From_Left_To_Right_Pressing_Left_Should_Remove_Selection_Moving_Caret_To_Start_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Left, KeyModifiers.None);

                Assert.Equal(selectionStart, tb.SelectionStart);
                Assert.Equal(selectionStart, tb.SelectionEnd);
                Assert.Equal(selectionStart, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(4, 2)]
        [InlineData(4, 0)]
        [InlineData(6, 2)]
        [InlineData(6, 0)]
        [InlineData(4, 3)]
        public void When_Selection_From_Right_To_Left_Pressing_Right_Should_Remove_Selection_Moving_Caret_To_Start_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Right, KeyModifiers.None);

                Assert.Equal(selectionStart, tb.SelectionStart);
                Assert.Equal(selectionStart, tb.SelectionEnd);
                Assert.Equal(selectionStart, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(4, 2)]
        [InlineData(4, 0)]
        [InlineData(6, 2)]
        [InlineData(6, 0)]
        [InlineData(4, 3)]
        public void When_Selection_From_Right_To_Left_Pressing_Left_Should_Remove_Selection_Moving_Caret_To_End_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Left, KeyModifiers.None);

                Assert.Equal(selectionEnd, tb.SelectionStart);
                Assert.Equal(selectionEnd, tb.SelectionEnd);
                Assert.Equal(selectionEnd, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        public void When_Select_All_From_Position_Left_Should_Remove_Selection_Moving_Caret_To_Start(int caretIndex)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = caretIndex;

                RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
                RaiseKeyEvent(tb, Key.Left, KeyModifiers.None);

                Assert.Equal(0, tb.SelectionStart);
                Assert.Equal(0, tb.SelectionEnd);
                Assert.Equal(0, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        public void When_Select_All_From_Position_Right_Should_Remove_Selection_Moving_Caret_To_End(int caretIndex)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = caretIndex;

                RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
                RaiseKeyEvent(tb, Key.Right, KeyModifiers.None);

                Assert.Equal(tb.Text.Length, tb.SelectionStart);
                Assert.Equal(tb.Text.Length, tb.SelectionEnd);
                Assert.Equal(tb.Text.Length, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(0, 4)]
        [InlineData(2, 6)]
        [InlineData(0, 6)]
        [InlineData(3, 4)]
        public void When_Selection_From_Left_To_Right_Pressing_Up_Should_Remove_Selection_Moving_Caret_To_Start_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Up, KeyModifiers.None);

                Assert.Equal(selectionStart, tb.SelectionStart);
                Assert.Equal(selectionStart, tb.SelectionEnd);
                Assert.Equal(selectionStart, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(4, 2)]
        [InlineData(4, 0)]
        [InlineData(6, 2)]
        [InlineData(6, 0)]
        [InlineData(4, 3)]
        public void When_Selection_From_Right_To_Left_Pressing_Up_Should_Remove_Selection_Moving_Caret_To_End_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Up, KeyModifiers.None);

                Assert.Equal(selectionEnd, tb.SelectionStart);
                Assert.Equal(selectionEnd, tb.SelectionEnd);
                Assert.Equal(selectionEnd, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        public void When_Select_All_From_Position_Up_Should_Remove_Selection_Moving_Caret_To_Start(int caretIndex)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = caretIndex;

                RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
                RaiseKeyEvent(tb, Key.Up, KeyModifiers.None);

                Assert.Equal(0, tb.SelectionStart);
                Assert.Equal(0, tb.SelectionEnd);
                Assert.Equal(0, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(2, 4)]
        [InlineData(0, 4)]
        [InlineData(2, 6)]
        [InlineData(0, 6)]
        [InlineData(3, 4)]
        public void When_Selection_From_Left_To_Right_Pressing_Down_Should_Remove_Selection_Moving_Caret_To_End_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Down, KeyModifiers.None);

                Assert.Equal(selectionEnd, tb.SelectionStart);
                Assert.Equal(selectionEnd, tb.SelectionEnd);
                Assert.Equal(selectionEnd, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(4, 2)]
        [InlineData(4, 0)]
        [InlineData(6, 2)]
        [InlineData(6, 0)]
        [InlineData(4, 3)]
        public void When_Selection_From_Right_To_Left_Pressing_Down_Should_Remove_Selection_Moving_Caret_To_Start_Of_Previous_Selection(int selectionStart, int selectionEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = selectionStart;
                tb.SelectionStart = selectionStart;
                tb.SelectionEnd = selectionEnd;

                RaiseKeyEvent(tb, Key.Down, KeyModifiers.None);

                Assert.Equal(selectionStart, tb.SelectionStart);
                Assert.Equal(selectionStart, tb.SelectionEnd);
                Assert.Equal(selectionStart, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        public void When_Select_All_From_Position_Down_Should_Remove_Selection_Moving_Caret_To_End(int caretIndex)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABCDEF"
                };

                tb.Measure(Size.Infinity);
                tb.CaretIndex = caretIndex;

                RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
                RaiseKeyEvent(tb, Key.Down, KeyModifiers.None);

                Assert.Equal(tb.Text.Length, tb.SelectionStart);
                Assert.Equal(tb.Text.Length, tb.SelectionEnd);
                Assert.Equal(tb.Text.Length, tb.CaretIndex);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(8)]
        public void When_Selecting_Multiline_Selection_Should_Be_Extended_With_Up_Arrow_Key_Till_Start_Of_Text(int caretOffsetFromEnd)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = """
                           AAAAAA
                           BBBB
                           CCCCCCCC
                           """,
                    AcceptsReturn = true
                };
                tb.ApplyTemplate();
                tb.Measure(Size.Infinity);
                tb.CaretIndex = tb.Text.Length - caretOffsetFromEnd;

                RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);
                RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);
                RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);
                RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);

                Assert.Equal(0, tb.SelectionEnd);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(6)]
        public void When_Selecting_Multiline_Selection_Should_Be_Extended_With_Down_Arrow_Key_Till_End_Of_Text(int caretOffsetFromStart)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = """
                           AAAAAA
                           BBBB
                           CCCCCCCC
                           """,
                    AcceptsReturn = true
                };
                tb.ApplyTemplate();
                tb.Measure(Size.Infinity);
                tb.CaretIndex = caretOffsetFromStart;

                RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);
                RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);
                RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);
                RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);

                Assert.Equal(tb.Text.Length, tb.SelectionEnd);
            }
        }

        [Fact]
        public void TextBox_In_AdornerLayer_Will_Not_Cause_Collection_Modified_In_VisualLayerManager_Measure()
        {
            using (UnitTestApplication.Start(Services))
            {
                var button = new Button();
                var root = new TestRoot()
                {
                    Child = new VisualLayerManager()
                    {
                        Child = button
                    }
                };
                var adorner = new TextBox { Template = CreateTemplate(), Text = "a" };

                var adornerLayer = AdornerLayer.GetAdornerLayer(button);
                Assert.NotNull(adornerLayer);
                adornerLayer.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, button);

                root.Measure(Size.Infinity);
            }
        }

        [Fact]
        public void TextBox_In_AdornerLayer_Will_Not_Cause_Collection_Modified_In_VisualLayerManager_Arrange()
        {
            using (UnitTestApplication.Start(Services))
            {
                var button = new Button();
                var visualLayerManager = new VisualLayerManager() { Child = button };
                var root = new TestRoot()
                {
                    Child = visualLayerManager
                };
                var adorner = new TextBox { Template = CreateTemplate(), Text = "a" };
                var adornerLayer = AdornerLayer.GetAdornerLayer(button);
                Assert.NotNull(adornerLayer);

                root.Measure(new Size(10, 10));

                adornerLayer.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, button);

                root.Arrange(new Rect(0, 0, 10, 10));
            }
        }

        [Theory]
        [InlineData("A\nBB\nCCC\nDDDD", 0, 0)]
        [InlineData("A\nBB\nCCC\nDDDD", 1, 2)]
        [InlineData("A\nBB\nCCC\nDDDD", 2, 5)]
        [InlineData("A\nBB\nCCC\nDDDD", 3, 9)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 0, 0)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 1, 5)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 2, 11)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 3, 17)]
        public void Should_Scroll_Caret_To_Line(string text, int targetLineIndex, int expectedCaretIndex)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = text
                };
                tb.ApplyTemplate();
                tb.ScrollToLine(targetLineIndex);
                Assert.Equal(expectedCaretIndex, tb.CaretIndex);
            }
        }

        [Fact]
        public void Should_Throw_ArgumentOutOfRange()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = string.Empty
                };
                tb.ApplyTemplate();

                Assert.Throws<ArgumentOutOfRangeException>(() => tb.ScrollToLine(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => tb.ScrollToLine(1));
            }
        }

        [Fact]
        public void InputMethodClient_SurroundingText_Returns_Empty_For_Empty_Line()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "",
                CaretIndex = 0
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var client = eventArgs.Client;
            Assert.NotNull(client);
            Assert.Equal(string.Empty, client.SurroundingText);
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_Can_Read_Document_Range()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "Line1\nLine2",
                CaretIndex = 0
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            Assert.Equal(0, structured.DocumentStart.Offset);
            Assert.Equal(textBox.Text!.Length, structured.DocumentEnd.Offset);
            Assert.Equal(textBox.Text, structured.GetText(structured.DocumentRange));

            var backwardPointer = structured.CreatePointer(2, LogicalDirection.Backward);
            Assert.Equal(2, backwardPointer.Offset);
            Assert.Equal(LogicalDirection.Backward, backwardPointer.Gravity);
        }

        private static ITextNavigation GetNavigation(TextBox textBox)
        {
            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);
            return Assert.IsAssignableFrom<ITextNavigation>(eventArgs.Client);
        }

        [Fact]
        public void InputMethodClient_TextNavigation_Resolves_Offsets_And_Reads_Text()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "Hello world", CaretIndex = 0 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            Assert.Equal(0, nav.DocumentStart.Offset);
            Assert.Equal(11, nav.DocumentEnd.Offset);

            // Resolve a platform offset relative to the produced DocumentStart anchor.
            var range = nav.GetRange(nav.GetPosition(nav.DocumentStart, 6), nav.DocumentEnd);
            var text = nav.GetText(range);

            Assert.Equal("world", text);
            Assert.Equal(range.End.Offset - range.Start.Offset, text.Length); // gapless invariant
        }

        [Fact]
        public void InputMethodClient_TextNavigation_GetPosition_Clamps_And_GetRange_Normalizes()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "abc", CaretIndex = 0 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            Assert.Equal(3, nav.GetPosition(nav.DocumentStart, 100).Offset);
            Assert.Equal(0, nav.GetPosition(nav.DocumentEnd, -100).Offset);

            var a = nav.GetPosition(nav.DocumentStart, 2);
            var b = nav.GetPosition(nav.DocumentStart, 1);
            var range = nav.GetRange(a, b); // arguments out of order are normalized

            Assert.Equal(1, range.Start.Offset);
            Assert.Equal(2, range.End.Offset);
            Assert.Equal(-1, nav.GetOffset(a, b));
        }

        [Fact]
        public void InputMethodClient_TextNavigation_Enclosing_And_Move_By_Word()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "foo bar", CaretIndex = 0 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            var inWord = nav.GetPosition(nav.DocumentStart, 1); // inside "foo"
            var word = nav.GetRangeEnclosing(inWord, TextUnit.Word);
            Assert.Equal(0, word.Start.Offset);
            Assert.Equal(3, word.End.Offset);

            // A single word forward from the start lands on the word-end boundary.
            Assert.Equal(3, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1).Offset);

            var whole = nav.GetRangeEnclosing(inWord, TextUnit.Document);
            Assert.Equal(0, whole.Start.Offset);
            Assert.Equal(7, whole.End.Offset);
        }

        [Fact]
        public void InputMethodClient_TextNavigation_TextChanged_Reports_Delta_And_Bumps_Version()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "Hello", CaretIndex = 5 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            TextChange? captured = null;
            nav.TextChanged += (_, c) => captured = c;
            var startVersion = nav.DocumentVersion;

            var structured = (IStructuredTextInput)nav;
            structured.ReplaceText(structured.CreateRange(structured.DocumentEnd, structured.DocumentEnd), "!");

            Assert.NotNull(captured);
            Assert.Equal(5, captured!.Value.Position.Offset);
            Assert.Equal(0, captured.Value.OldLength);
            Assert.Equal(1, captured.Value.NewLength);
            Assert.True(nav.DocumentVersion > startVersion);

            // The inserted text is read on demand from the change position.
            var inserted = nav.GetRange(
                captured.Value.Position,
                nav.GetPosition(captured.Value.Position, captured.Value.NewLength));
            Assert.Equal("!", nav.GetText(inserted));
        }

        [Fact]
        public void InputMethodClient_TextNavigation_Rejects_Foreign_Pointer()
        {
            using var _ = UnitTestApplication.Start(Services);

            var a = new TextBox { Template = CreateTemplate(), Text = "aaa" };
            a.ApplyTemplate();
            var b = new TextBox { Template = CreateTemplate(), Text = "bbb" };
            b.ApplyTemplate();

            var navA = GetNavigation(a);
            var navB = GetNavigation(b);

            Assert.Throws<ArgumentException>(() => navA.GetOffset(navA.DocumentStart, navB.DocumentStart));
        }

        [Fact]
        public void InputMethodClient_TextNavigation_Character_Unit_Is_Grapheme_Aware()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "áb", CaretIndex = 0 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            // "a" + combining acute (U+0301) is a single grapheme spanning two code units.
            var grapheme = nav.GetRangeEnclosing(nav.DocumentStart, TextUnit.Character);
            Assert.Equal(0, grapheme.Start.Offset);
            Assert.Equal(2, grapheme.End.Offset);

            var afterTwo = nav.GetPosition(nav.DocumentStart, TextUnit.Character, 2);
            Assert.Equal(3, afterTwo.Offset);
            Assert.Equal(2, nav.GetPosition(afterTwo, TextUnit.Character, -1).Offset);
        }

        [Fact]
        public void InputMethodClient_TextNavigation_Word_Unit_Uses_Uax29()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "don't stop", CaretIndex = 0 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            // UAX-29 keeps the contraction together; the old ASCII heuristic split it at the apostrophe.
            var word = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 2), TextUnit.Word);
            Assert.Equal(0, word.Start.Offset);
            Assert.Equal(5, word.End.Offset);

            // Word boundaries: 0 | "don't" 5 | " " 6 | "stop" 10.
            Assert.Equal(5, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1).Offset);
            Assert.Equal(0, nav.GetPosition(nav.GetPosition(nav.DocumentStart, 5), TextUnit.Word, -1).Offset);
        }

        [Fact]
        public void AutomationTextRange_Over_TextNavigation_Expands_Moves_And_Reads()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "foo bar", CaretIndex = 0 };
            textBox.ApplyTemplate();
            var nav = GetNavigation(textBox);

            // The document range reads the whole text.
            var doc = new Avalonia.Automation.AutomationTextRange(nav, nav.DocumentStart, nav.DocumentEnd);
            Assert.Equal("foo bar", doc.GetText(-1));

            // A degenerate range inside "foo" expands to the enclosing word.
            var inFoo = nav.GetPosition(nav.DocumentStart, 1);
            var word = new Avalonia.Automation.AutomationTextRange(nav, inFoo, inFoo);
            word.ExpandToEnclosingUnit(TextUnit.Word);
            Assert.Equal("foo", word.GetText(-1));

            // Clone is independent; moving the clone's end back one character does not affect the original.
            var clone = (Avalonia.Automation.AutomationTextRange)word.Clone();
            clone.MoveEndpointByUnit(Avalonia.Automation.Provider.TextRangeEndpoint.End, TextUnit.Character, -1);
            Assert.Equal("fo", clone.GetText(-1));
            Assert.Equal("foo", word.GetText(-1));

            // The original word's end follows the clone's shortened end.
            Assert.True(word.CompareEndpoints(
                Avalonia.Automation.Provider.TextRangeEndpoint.End,
                clone,
                Avalonia.Automation.Provider.TextRangeEndpoint.End) > 0);
        }

        [Fact]
        public void TextBoxAutomationPeer_Exposes_Text_Via_ITextProvider()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "foo bar", CaretIndex = 0 };
            textBox.ApplyTemplate();

            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(textBox);
            var textProvider = Assert.IsAssignableFrom<Avalonia.Automation.Provider.ITextProvider>(peer);

            Assert.Equal("foo bar", textProvider.DocumentRange.GetText(-1));
            Assert.Equal(Avalonia.Automation.Provider.SupportedTextSelection.Single, textProvider.SupportedTextSelection);

            textBox.SelectionStart = 0;
            textBox.SelectionEnd = 3;

            var selection = textProvider.GetSelection();
            Assert.Single(selection);
            Assert.Equal("foo", selection[0].GetText(-1));
        }

        [Fact]
        public void TextBoxAutomationPeer_ITextRange_Select_Updates_Selection()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "foo bar", CaretIndex = 0 };
            textBox.ApplyTemplate();

            var textProvider = Assert.IsAssignableFrom<Avalonia.Automation.Provider.ITextProvider>(
                Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(textBox));

            // Expand the document range to the first word and select it through the UIA-shaped range.
            var range = textProvider.DocumentRange;
            range.ExpandToEnclosingUnit(TextUnit.Word);
            range.Select();

            Assert.Equal(0, textBox.SelectionStart);
            Assert.Equal(3, textBox.SelectionEnd);
        }

        [Fact]
        public void TextBoxAutomationPeer_IAccessibleText_GetSelection_Round_Trips_The_Control_Selection()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "foo bar", CaretIndex = 0 };
            textBox.ApplyTemplate();

            var peer = Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(textBox);
            var accessible = Assert.IsAssignableFrom<Avalonia.Automation.Provider.IAccessibleText>(
                peer.GetProvider<Avalonia.Automation.Provider.IAccessibleText>());

            // Selection-read reflects the control and normalizes reversed anchors.
            textBox.SelectionStart = 7;
            textBox.SelectionEnd = 4;
            var selection = accessible.GetSelection();
            Assert.Equal(4, selection.Start.Offset);
            Assert.Equal(7, selection.End.Offset);
            Assert.False(selection.IsEmpty);

            // A collapsed selection (the caret) is empty.
            textBox.SelectionStart = textBox.SelectionEnd = 2;
            Assert.True(accessible.GetSelection().IsEmpty);

            // SetSelection writes back through the control.
            accessible.SetSelection(accessible.GetRange(
                accessible.GetPosition(accessible.DocumentStart, 0),
                accessible.GetPosition(accessible.DocumentStart, 3)));
            Assert.Equal(0, textBox.SelectionStart);
            Assert.Equal(3, textBox.SelectionEnd);
        }

        [Fact]
        public void TextBoxAutomationPeer_IAccessibleText_Reports_Font_Attributes_Over_A_Uniform_Run()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "foo bar",
                FontFamily = new FontFamily("Courier New"),
                FontSize = 17,
                FontWeight = FontWeight.Bold,
                FontStyle = FontStyle.Italic,
                Foreground = Brushes.Red,
            };
            textBox.ApplyTemplate();

            var accessible = Assert.IsAssignableFrom<Avalonia.Automation.Provider.IAccessibleText>(
                Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(textBox)
                    .GetProvider<Avalonia.Automation.Provider.IAccessibleText>());

            var (attributes, run) = accessible.GetTextAttributes(accessible.DocumentStart);

            Assert.Equal("Courier New", attributes[TextAttribute.FontFamily]);
            Assert.Equal(17d, attributes[TextAttribute.FontSize]);
            Assert.Equal(FontWeight.Bold, attributes[TextAttribute.FontWeight]);
            Assert.Equal(FontStyle.Italic, attributes[TextAttribute.FontStyle]);
            Assert.Equal(Colors.Red, attributes[TextAttribute.Foreground]);
            Assert.Equal(false, attributes[TextAttribute.IsReadOnly]);

            // Uniform formatting: the run spans the whole document.
            Assert.Equal(0, run.Start.Offset);
            Assert.Equal(7, run.End.Offset);

            // The UIA-shaped range reports the same value uniformly over any sub-range.
            var word = new Avalonia.Automation.AutomationTextRange(
                accessible, accessible.DocumentStart, accessible.GetPosition(accessible.DocumentStart, 3));
            Assert.Equal(FontWeight.Bold, word.GetAttributeValue(TextAttribute.FontWeight));
            Assert.Equal(Colors.Red, word.GetAttributeValue(TextAttribute.Foreground));
        }

        [Fact]
        public void TextBoxAutomationPeer_GetPositionFromPoint_Hit_Tests_Back_To_The_Character()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox { Template = CreateTemplate(), Text = "Hello world" };

            var impl = CreateMockTopLevelImpl();
            var topLevel = new TestTopLevel(impl.Object) { Template = CreateTopLevelTemplate() };
            topLevel.Content = textBox;
            topLevel.ApplyTemplate();
            topLevel.LayoutManager.ExecuteInitialLayoutPass();
            textBox.Measure(Size.Infinity);

            var accessible = Assert.IsAssignableFrom<Avalonia.Automation.Provider.IAccessibleText>(
                Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(textBox)
                    .GetProvider<Avalonia.Automation.Provider.IAccessibleText>());

            // Top-level bounding rect of 'w' (offset 6 in "Hello world").
            var wChar = accessible.GetRange(
                accessible.GetPosition(accessible.DocumentStart, 6),
                accessible.GetPosition(accessible.DocumentStart, 7));
            var rects = accessible.GetBoundingRectangles(wChar);
            Assert.NotEmpty(rects);

            // A point just inside that rect hit-tests back into the same character (the inverse of
            // GetBoundingRectangles round-trips).
            var probe = new Point(rects[0].X + 1, rects[0].Center.Y);
            var hit = accessible.GetPositionFromPoint(probe);

            Assert.NotNull(hit);
            Assert.Equal("w", accessible.GetText(accessible.GetRangeEnclosing(hit!, TextUnit.Character)));
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_Composition_Mutates_Text_And_Commits()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "Hello",
                CaretIndex = 5
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            structured.SetCompositionText("!", 1);

            Assert.Equal("Hello!", textBox.Text);
            Assert.NotNull(structured.CompositionRange);
            Assert.Equal(5, structured.CompositionRange!.Start.Offset);
            Assert.Equal(6, structured.CompositionRange!.End.Offset);

            structured.CommitComposition();

            Assert.Null(structured.CompositionRange);
            Assert.Equal("Hello!", textBox.Text);
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_ReplaceText_Replaces_Selection()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "abcde",
                SelectionStart = 1,
                SelectionEnd = 4
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            structured.ReplaceText(structured.Selection, "ZZ");

            Assert.Equal("aZZe", textBox.Text);
            Assert.Equal(3, textBox.SelectionStart);
            Assert.Equal(3, textBox.SelectionEnd);
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_ReplaceText_Raises_TextChanged()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "abc",
                CaretIndex = 3
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            var eventCount = 0;
            structured.TextChanged += (_, _) => eventCount++;

            var end = structured.DocumentEnd;
            structured.ReplaceText(structured.CreateRange(end, end), "!");

            Assert.Equal("abc!", textBox.Text);
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_SetCompositionText_Raises_CompositionChanged()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "text",
                CaretIndex = 4
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            var eventCount = 0;
            structured.CompositionChanged += (_, _) => eventCount++;

            structured.SetCompositionText("!", 1);
            structured.CommitComposition();

            Assert.Equal("text!", textBox.Text);
            Assert.Equal(2, eventCount);
            Assert.Null(structured.CompositionRange);
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_ReplaceText_Raises_CaretPositionChanged()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "abc",
                CaretIndex = 3
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            var eventCount = 0;
            structured.CaretPositionChanged += (_, _) => eventCount++;

            var end = structured.DocumentEnd;
            structured.ReplaceText(structured.CreateRange(end, end), "!");

            Assert.Equal("abc!", textBox.Text);
            Assert.True(eventCount > 0);
        }

        [Fact]
        public void InputMethodClient_StructuredTextInput_SetSelection_Raises_CaretPositionChanged()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "abcdef",
                CaretIndex = 6
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var structured = Assert.IsAssignableFrom<IStructuredTextInput>(eventArgs.Client);

            var eventCount = 0;
            structured.CaretPositionChanged += (_, _) => eventCount++;

            var start = structured.CreatePointer(1, LogicalDirection.Forward);
            var end = structured.CreatePointer(4, LogicalDirection.Backward);
            structured.Selection = structured.CreateRange(start, end);

            Assert.Equal(1, textBox.SelectionStart);
            Assert.Equal(4, textBox.SelectionEnd);
            Assert.True(eventCount > 0);
        }

        [Fact]
        public void Backspace_Should_Delete_Last_Character_In_Line_And_Keep_Caret_On_Same_Line()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "a\nb",
                CaretIndex = 3
            };
            textBox.ApplyTemplate();

            var topLevel = new TestTopLevel(CreateMockTopLevelImpl().Object)
            {
                Template = CreateTopLevelTemplate(),
                Content = textBox
            };
            topLevel.ApplyTemplate();
            topLevel.LayoutManager.ExecuteInitialLayoutPass();

            var textPresenter = textBox.FindDescendantOfType<TextPresenter>();
            Assert.NotNull(textPresenter);

            var oldCaretY = textPresenter.GetCursorRectangle().Top;
            Assert.NotEqual(0, oldCaretY);

            RaiseKeyEvent(textBox, Key.Back, KeyModifiers.None);

            Assert.Equal("a\n", textBox.Text);
            Assert.Equal(2, textBox.CaretIndex);
            Assert.Equal(2, textPresenter.CaretIndex);

            var caretY = textPresenter.GetCursorRectangle().Top;
            Assert.Equal(oldCaretY, caretY);
        }

        [Fact]
        public void Losing_Focus_Should_Not_Reset_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ClearSelectionOnLostFocus = false
                };

                target1.ApplyTemplate();

                var target2 = new TextBox
                {
                    Template = CreateTemplate(),
                };

                target2.ApplyTemplate();

                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                var root = new TestRoot() { Child = sp };

                target1.SelectionStart = 0;
                target1.SelectionEnd = 4;

                target1.Focus();

                Assert.True(target1.IsFocused);

                Assert.Equal("1234", target1.SelectedText);

                target2.Focus();

                Assert.Equal("1234", target1.SelectedText);
            }
        }

        [Fact]
        public void Backspace_Should_Delete_CRLFNewline_Character_At_Once()
        {
            using var _ = UnitTestApplication.Start(Services);
            var target = new TextBox
            {
                Template = CreateTemplate(),
                Text = $"First\r\nSecond",
                CaretIndex = 7
            };
            target.ApplyTemplate();

            // (First\r\nSecond)
            RaiseKeyEvent(target, Key.Back, KeyModifiers.None);
            // (FirstSecond)

            Assert.Equal("FirstSecond", target.Text);
        }

        [Fact]
        public void PlaceholderForeground_Can_Be_Set()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    PlaceholderText = "Enter text",
                    PlaceholderForeground = Brushes.Red
                };

                target.ApplyTemplate();

                Assert.Equal(Brushes.Red, target.PlaceholderForeground);
            }
        }

        [Fact]
        public void PlaceholderForeground_Defaults_To_Null()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    PlaceholderText = "Enter text"
                };

                target.ApplyTemplate();

                Assert.Null(target.PlaceholderForeground);
            }
        }

        [Fact]
        public void PlaceholderForeground_Can_Be_Set_To_Null()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    PlaceholderText = "Enter text",
                    PlaceholderForeground = Brushes.Blue
                };

                target.ApplyTemplate();

                target.PlaceholderForeground = null;

                Assert.Null(target.PlaceholderForeground);
            }
        }

        private static TestServices FocusServices => TestServices.MockThreadingInterface.With(
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new TestFontManager());

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new TestFontManager(),
            assetLoader: new StandardAssetLoader());

        internal static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<TextBox>((control, scope) =>
            new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
                Content = new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = nameof(TextPresenter.Text),
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                    [!!TextPresenter.CaretIndexProperty] = new Binding
                    {
                        Path = nameof(TextPresenter.CaretIndex),
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    }
                }.RegisterInNameScope(scope)
            }.RegisterInNameScope(scope));
        }

        private static void AssertReadOnlyHotkeyLeavesStateUntouched(
            TextBox textBox,
            IReadOnlyList<KeyGesture> gestures,
            bool handled)
        {
            Assert.NotEmpty(gestures);
            var gesture = gestures[0];
            AssertReadOnlyHotkeyLeavesStateUntouched(textBox, gesture.Key, gesture.KeyModifiers, handled);
        }

        private static void AssertReadOnlyHotkeyLeavesStateUntouched(
            TextBox textBox,
            Key key,
            KeyModifiers inputModifiers,
            bool handled)
        {
            var originalText = textBox.Text;
            var originalCaretIndex = textBox.CaretIndex;
            var originalSelectionStart = textBox.SelectionStart;
            var originalSelectionEnd = textBox.SelectionEnd;
            var originalCanUndo = textBox.CanUndo;
            var originalCanRedo = textBox.CanRedo;

            var args = RaiseKeyEvent(textBox, key, inputModifiers);

            Assert.Equal(handled, args.Handled);
            Assert.Equal(originalText, textBox.Text);
            Assert.Equal(originalCaretIndex, textBox.CaretIndex);
            Assert.Equal(originalSelectionStart, textBox.SelectionStart);
            Assert.Equal(originalSelectionEnd, textBox.SelectionEnd);
            Assert.Equal(originalCanUndo, textBox.CanUndo);
            Assert.Equal(originalCanRedo, textBox.CanRedo);
        }

        private static KeyEventArgs RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
        {
            var args = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            };

            textBox.RaiseEvent(args);

            return args;
        }

        private static void RaiseTextEvent(TextBox textBox, string text)
        {
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        }

        private class Class1 : NotifyingBase
        {
            private int _foo;
            private string? _bar;

            public int Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }

            public string? Bar
            {
                get { return _bar; }
                set { _bar = value; RaisePropertyChanged(); }
            }
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {
        }

        private static Mock<ITopLevelImpl> CreateMockTopLevelImpl()
        {
            var clipboard = new Mock<ITopLevelImpl>();
            clipboard.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            clipboard.Setup(r => r.TryGetFeature(typeof(IClipboard)))
                .Returns(new Clipboard(new HeadlessClipboardImplStub()));
            clipboard.SetupGet(x => x.RenderScaling).Returns(1);
            return clipboard;
        }

        private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
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
