#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests : ScopedTestBase
    {
        [Fact]
        public void Spell_Check_Uses_Platform_Default_For_Natural_Text_Inputs()
        {
            Assert.Null(TextInputOptions.GetIsSpellCheckEnabled(new TextBox()));
            Assert.Null(TextInputOptions.GetIsSpellCheckEnabled(new AutoCompleteBox()));
            Assert.Null(TextInputOptions.GetIsSpellCheckEnabled(new ComboBox()));
        }

        [Fact]
        public void Spell_Check_Is_Disabled_By_Default_For_Formatted_Text_Inputs()
        {
            Assert.False(TextInputOptions.GetIsSpellCheckEnabled(new MaskedTextBox()));
            Assert.False(TextInputOptions.GetIsSpellCheckEnabled(new NumericUpDown()));
            Assert.False(TextInputOptions.GetIsSpellCheckEnabled(new CalendarDatePicker()));
        }

        [Fact]
        public void Spell_Check_Can_Be_Explicitly_Enabled_For_Formatted_Text_Inputs()
        {
            var target = new NumericUpDown();

            TextInputOptions.SetIsSpellCheckEnabled(target, true);

            Assert.True(TextInputOptions.GetIsSpellCheckEnabled(target));
        }

        [Fact]
        public void Context_Requested_Populates_Spell_Check_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new SpellCheckResult(0, 3, "Ths") },
                        new[] { "This", "The" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.True(target.HasSpellCheckSuggestions);
                Assert.Equal(new[] { "This", "The" }, target.SpellCheckSuggestions);
            }
        }

        [Fact]
        public void Context_Requested_Limits_Spell_Check_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new SpellCheckResult(0, 3, "Ths") },
                        new[]
                        {
                            "Suggestion 1",
                            "Suggestion 2",
                            "Suggestion 3",
                            "Suggestion 4",
                            "Suggestion 5",
                            "Suggestion 6",
                            "Suggestion 7",
                            "Suggestion 8",
                            "Suggestion 9",
                            "Suggestion 10"
                        }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(
                    new[]
                    {
                        "Suggestion 1",
                        "Suggestion 2",
                        "Suggestion 3",
                        "Suggestion 4",
                        "Suggestion 5",
                        "Suggestion 6",
                        "Suggestion 7",
                        "Suggestion 8"
                    },
                    target.SpellCheckSuggestions);
            }
        }

        [Fact]
        public void Pointer_Context_Requested_Populates_Spell_Check_Suggestions_For_Clicked_Word()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var provider = new TestSpellCheckProvider(
                    new[]
                    {
                        new SpellCheckResult(0, 3, "Ths"),
                        new SpellCheckResult(4, 4, "wrng")
                    },
                    new[] { "wrong" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "Ths wrng",
                    Width = 80,
                    Height = 32
                };
                var window = new Window
                {
                    Content = target,
                    Width = 120,
                    Height = 80
                };
                window.Show();
                target.CaretIndex = 1;
                var presenter = GetVisualDescendant<TextPresenter>(target);
                var root = Assert.IsAssignableFrom<Visual>(presenter.VisualRoot);
                var pointInPresenter = new Point(
                    Math.Max(presenter.TextLayout.Width - 1, 0),
                    presenter.TextLayout.Height / 2);
                var pointInRoot = presenter.TranslatePoint(pointInPresenter, root);

                Assert.NotNull(pointInRoot);

                var contextRequested = CreateContextRequestedAtRootPoint(presenter, root, pointInRoot.Value);

                presenter.RaiseEvent(contextRequested);
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.True(target.HasSpellCheckSuggestions);
                Assert.Equal("wrng", provider.LastSuggestedWord);

                target.ApplySpellCheckSuggestion("wrong");

                Assert.Equal("Ths wrong", target.Text);
            }
        }

        [Fact]
        public void Apply_Spell_Check_Suggestion_Replaces_Misspelled_Word()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new SpellCheckResult(0, 3, "Ths") },
                        new[] { "This" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                target.ApplySpellCheckSuggestion("This");

                Assert.Equal("This sample", target.Text);
                Assert.False(target.HasSpellCheckSuggestions);
            }
        }

        [Fact]
        public void Context_Requested_Away_From_Misspelling_Clears_Spell_Check_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new SpellCheckResult(0, 3, "Ths") },
                        new[] { "This" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.True(target.HasSpellCheckSuggestions);

                target.CaretIndex = target.Text!.Length;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.False(target.HasSpellCheckSuggestions);
                Assert.Empty(target.SpellCheckSuggestions);
            }
        }

        [Fact]
        public void Spell_Check_Manager_Is_Not_Created_Without_Platform_Provider()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.False(target.HasSpellCheckManager);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Spell_Check_Manager_Is_Not_Created_When_Language_Is_Unsupported()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "Ths") },
                    new[] { "This" },
                    isLanguageSupported: false);

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.False(target.HasSpellCheckManager);
                Assert.Equal(0, provider.CheckCount);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Spell_Check_Manager_Is_Not_Created_When_Spell_Check_Is_Disabled()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "Ths sample",
                    textBox => TextInputOptions.SetIsSpellCheckEnabled(textBox, false));

                Assert.False(target.HasSpellCheckManager);
                Assert.Equal(0, provider.CheckCount);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Spell_Check_Manager_Is_Not_Created_For_Number_Content()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "123") },
                    new[] { "one two three" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "123",
                    textBox =>
                    {
                        TextInputOptions.SetContentType(textBox, TextInputContentType.Number);
                        TextInputOptions.SetIsSpellCheckEnabled(textBox, true);
                    });

                Assert.False(target.HasSpellCheckManager);
                Assert.Equal(0, provider.CheckCount);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Spell_Check_Manager_Is_Released_When_Spell_Check_Is_Disabled()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.True(target.HasSpellCheckManager);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests());

                TextInputOptions.SetIsSpellCheckEnabled(target, false);

                Assert.False(target.HasSpellCheckManager);
                Assert.Equal(0, provider.CheckCount);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Pending_Spell_Check_Is_Canceled_When_Text_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new BlockingSpellCheckProvider();

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");
                var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
                timer.ForceFire();

                Assert.False(provider.CheckCancellationToken.IsCancellationRequested);

                target.Text = "This sample";

                Assert.True(provider.CheckCancellationToken.IsCancellationRequested);

                provider.Complete();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
            }
        }

        [Fact]
        public void Spell_Check_Results_Are_Cleared_When_Text_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Ths", GetDecoratedText(target));

                target.Text = "This sample";

                Assert.Empty(GetDecoratedText(target));
            }
        }

        [Fact]
        public void Spell_Check_Results_Are_Cleared_When_Provider_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Ths", GetDecoratedText(target));

                TextInputOptions.SetSpellCheckProvider(
                    target,
                    new TestSpellCheckProvider(Array.Empty<SpellCheckResult>(), Array.Empty<string>()));

                Assert.Empty(GetDecoratedText(target));
            }
        }

        [Fact]
        public void Pending_Spell_Check_Is_Canceled_When_Visible_Range_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new BlockingSpellCheckProvider();
                var text = CreateMisspelledLines(40, out _);

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    text,
                    textBox =>
                    {
                        textBox.Width = 200;
                        textBox.Height = 60;
                    });

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();

                var cancellation = provider.CheckCancellationToken;
                Assert.False(cancellation.IsCancellationRequested);

                var scrollViewer = GetVisualDescendant<ScrollViewer>(target);
                Assert.True(scrollViewer.ScrollBarMaximum.Y > 0);

                scrollViewer.Offset = scrollViewer.Offset.WithY(scrollViewer.ScrollBarMaximum.Y);
                topLevel.LayoutManager.ExecuteLayoutPass();

                Assert.True(cancellation.IsCancellationRequested);

                provider.Complete();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
            }
        }

        [Fact]
        public void Fast_Scroll_Checks_Latest_Visible_Range()
        {
            using (UnitTestApplication.Start(Services))
            {
                var text = CreateMisspelledLines(40, out var results);
                var provider = new TestSpellCheckProvider(results, Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    text,
                    textBox =>
                    {
                        textBox.Width = 200;
                        textBox.Height = 60;
                    });

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal(1, provider.CheckCount);

                var scrollViewer = GetVisualDescendant<ScrollViewer>(target);
                Assert.True(scrollViewer.ScrollBarMaximum.Y > 0);

                scrollViewer.Offset = scrollViewer.Offset.WithY(scrollViewer.ScrollBarMaximum.Y / 2);
                topLevel.LayoutManager.ExecuteLayoutPass();

                scrollViewer.Offset = scrollViewer.Offset.WithY(scrollViewer.ScrollBarMaximum.Y);
                topLevel.LayoutManager.ExecuteLayoutPass();

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(2, provider.CheckCount);
                Assert.Contains("wrng39", provider.LastCheckedText!);
                Assert.DoesNotContain("wrng00", provider.LastCheckedText!);
            }
        }

        [Fact]
        public void Large_Text_Checks_Visible_Text_Only()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    Array.Empty<SpellCheckResult>(),
                    Array.Empty<string>());
                var text = "Ths " + new string('a', 10_001);

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    text,
                    textBox =>
                    {
                        textBox.Width = 80;
                        textBox.Height = 40;
                    });

                Assert.True(target.HasSpellCheckManager);

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(1, provider.CheckCount);
                Assert.NotNull(provider.LastCheckedText);
                Assert.True(provider.LastCheckedText!.Length < text.Length);
            }
        }

        [Fact]
        public void Visible_Spell_Check_Range_Does_Not_Render_Partial_Word_Misspelling()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, int.MaxValue) },
                    Array.Empty<string>());
                var text = "mispelledlongwordmispelledlongword";

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    text,
                    textBox =>
                    {
                        textBox.Width = 40;
                        textBox.Height = 40;
                    });

                Assert.True(target.HasSpellCheckManager);

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(1, provider.CheckCount);
                Assert.NotNull(provider.LastCheckedText);
                Assert.True(provider.LastCheckedText!.Length < text.Length);
                Assert.Empty(GetDecoratedText(target));
            }
        }

        [Fact]
        public void Context_Requested_Checks_Large_Text_Context_Only()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new SpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths " + new string('a', 10_001));
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(1, provider.CheckCount);
                Assert.Equal("Ths", provider.LastCheckedText);
                Assert.True(target.HasSpellCheckManager);
                Assert.True(target.HasSpellCheckSuggestions);
            }
        }

        [Fact]
        public void Context_Requested_Merges_New_Result_With_Existing_Results()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[]
                    {
                        new SpellCheckResult(0, 3, "Ths"),
                        new SpellCheckResult(4, 4, "wrng")
                    },
                    new[] { "fixed" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths wrng");

                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal("Ths", GetDecoratedText(target));

                target.CaretIndex = 5;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal("Thswrng", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Spell_Check_Renders_Unsorted_Provider_Results_In_Text_Order()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[]
                    {
                        new SpellCheckResult(4, 4, "wrng"),
                        new SpellCheckResult(0, 3, "Ths")
                    },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths wrng");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal("Thswrng", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Spell_Check_Only_Renders_Visible_Misspellings()
        {
            using (UnitTestApplication.Start(Services))
            {
                var text = CreateMisspelledLines(40, out var results);
                var provider = new TestSpellCheckProvider(results, Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    text,
                    textBox =>
                    {
                        textBox.Width = 200;
                        textBox.Height = 60;
                    });

                Assert.Equal(0, provider.CheckCount);

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var scrollViewer = GetVisualDescendant<ScrollViewer>(target);
                Assert.True(scrollViewer.ScrollBarMaximum.Y > 0);

                var initialDecoratedText = GetDecoratedText(target);
                Assert.Contains("wrng00", initialDecoratedText);
                Assert.DoesNotContain("wrng39", initialDecoratedText);

                scrollViewer.Offset = scrollViewer.Offset.WithY(scrollViewer.ScrollBarMaximum.Y);
                topLevel.LayoutManager.ExecuteLayoutPass();
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var scrolledDecoratedText = GetDecoratedText(target);
                Assert.Contains("wrng39", scrolledDecoratedText);
                Assert.DoesNotContain("wrng00", scrolledDecoratedText);
            }
        }

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

        private static TextBox CreateTextBoxInTopLevel(string text, Action<TextBox>? configure = null)
        {
            return CreateTextBoxInTopLevelWithRoot(text, configure).Target;
        }

        private static (TextBox Target, TestTopLevel TopLevel) CreateTextBoxInTopLevelWithRoot(string text, Action<TextBox>? configure = null)
        {
            var target = new TextBox
            {
                Template = CreateTemplate(),
                Text = text
            };

            configure?.Invoke(target);

            var topLevel = new TestTopLevel(CreateMockTopLevelImpl().Object)
            {
                Template = CreateTopLevelTemplate(),
                Content = target
            };

            topLevel.ApplyTemplate();
            topLevel.LayoutManager.ExecuteInitialLayoutPass();

            return (target, topLevel);
        }

        private static string CreateMisspelledLines(int count, out IReadOnlyList<SpellCheckResult> results)
        {
            var text = new StringBuilder();
            var spellCheckResults = new List<SpellCheckResult>(count);

            for (var i = 0; i < count; i++)
            {
                var word = FormattableString.Invariant($"wrng{i:00}");

                spellCheckResults.Add(new SpellCheckResult(text.Length, word.Length, word));
                text.Append(word);
                text.Append('\n');
            }

            results = spellCheckResults;

            return text.ToString();
        }

        private static string GetDecoratedText(TextBox textBox)
        {
            var presenter = GetVisualDescendant<TextPresenter>(textBox);
            var decoratedText = new StringBuilder();

            foreach (var textLine in presenter.TextLayout.TextLines)
            {
                foreach (var textRun in textLine.TextRuns)
                {
                    if (textRun.Properties?.TextDecorations?.Count > 0)
                    {
                        decoratedText.Append(textRun.Text.ToString());
                    }
                }
            }

            return decoratedText.ToString();
        }

        private static T GetVisualDescendant<T>(Visual visual)
            where T : Visual
        {
            foreach (var descendant in visual.GetVisualDescendants())
            {
                if (descendant is T result)
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"Could not find visual descendant of type {typeof(T).Name}.");
        }

        private static void RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
        {
            textBox.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private static void RaiseTextEvent(TextBox textBox, string text)
        {
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        }

        private static ContextRequestedEventArgs CreateContextRequestedAtRootPoint(
            Interactive source,
            Visual root,
            Point point)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var pointerArgs = new PointerPressedEventArgs(
                source,
                pointer,
                root,
                point,
                0,
                new PointerPointProperties(RawInputModifiers.RightMouseButton, PointerUpdateKind.RightButtonPressed),
                KeyModifiers.None);

            return new ContextRequestedEventArgs(pointerArgs);
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

        private class TestSpellCheckProvider : ISpellCheckProvider
        {
            private readonly IReadOnlyList<SpellCheckResult> _results;
            private readonly IReadOnlyList<string> _suggestions;
            private readonly bool _isLanguageSupported;

            public TestSpellCheckProvider(
                IReadOnlyList<SpellCheckResult> results,
                IReadOnlyList<string> suggestions,
                bool isLanguageSupported = true)
            {
                _results = results;
                _suggestions = suggestions;
                _isLanguageSupported = isLanguageSupported;
            }

            public int CheckCount { get; private set; }

            public string? LastCheckedText { get; private set; }

            public string? LastSuggestedWord { get; private set; }

            public bool IsLanguageSupported(CultureInfo? culture) => _isLanguageSupported;

            public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
                ReadOnlySpan<char> text,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                CheckCount++;
                var textValue = text.ToString();
                LastCheckedText = textValue;

                if (_results.Count == 0)
                {
                    return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
                }

                List<SpellCheckResult>? results = null;

                for (var i = 0; i < _results.Count; i++)
                {
                    var result = _results[i];

                    if (result.Word is { } word)
                    {
                        var start = textValue.IndexOf(word, StringComparison.Ordinal);

                        if (start < 0)
                        {
                            continue;
                        }

                        results ??= new List<SpellCheckResult>();
                        results.Add(result with { Start = start, Length = word.Length });
                    }
                    else if (result.Start >= 0 && result.Start < textValue.Length)
                    {
                        results ??= new List<SpellCheckResult>();
                        results.Add(result);
                    }
                }

                return new ValueTask<IReadOnlyList<SpellCheckResult>>(
                    results is null || results.Count == 0 ? Array.Empty<SpellCheckResult>() : results);
            }

            public ValueTask<IReadOnlyList<string>> SuggestAsync(
                string word,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                LastSuggestedWord = word;
                return new ValueTask<IReadOnlyList<string>>(_suggestions);
            }
        }

        private class BlockingSpellCheckProvider : ISpellCheckProvider
        {
            private readonly TaskCompletionSource<IReadOnlyList<SpellCheckResult>> _checkCompletion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public CancellationToken CheckCancellationToken { get; private set; }

            public bool IsLanguageSupported(CultureInfo? culture) => true;

            public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
                ReadOnlySpan<char> text,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                CheckCancellationToken = cancellationToken;
                return new ValueTask<IReadOnlyList<SpellCheckResult>>(_checkCompletion.Task);
            }

            public ValueTask<IReadOnlyList<string>> SuggestAsync(
                string word,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
            }

            public void Complete()
            {
                _checkCompletion.SetResult(Array.Empty<SpellCheckResult>());
            }
        }
    }
}
