using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public partial class TextBoxTests
    {
        [Fact]
        public void Spell_Check_Is_Disabled_By_Default_For_Natural_Text_Inputs()
        {
            Assert.False(SpellCheck.GetIsEnabled(new TextBox()));
            Assert.False(SpellCheck.GetIsEnabled(new AutoCompleteBox()));
            Assert.False(SpellCheck.GetIsEnabled(new ComboBox()));
        }

        [Fact]
        public void Spell_Check_Is_Opt_In_By_Default()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "Ths sample",
                    tb => SpellCheck.SetIsEnabled(tb, false));

                Assert.False(target.HasSpellCheckManager);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
                Assert.Equal(0, provider.CheckCount);
            }
        }

        [Fact]
        public void Spell_Check_Is_Inherited_From_Parent_Scope()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "Ths sample"
                };

                var root = new Panel { Children = { target } };
                SpellCheck.SetIsEnabled(root, true);

                var topLevel = new TestTopLevel(CreateMockTopLevelImpl().Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = root
                };

                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                Assert.True(target.HasSpellCheckManager);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Ths", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Spell_Check_Is_Disabled_By_Default_For_Formatted_Text_Inputs()
        {
            Assert.False(SpellCheck.GetIsEnabled(new MaskedTextBox()));
            Assert.False(SpellCheck.GetIsEnabled(new NumericUpDown()));
            Assert.False(SpellCheck.GetIsEnabled(new CalendarDatePicker()));
        }

        [Fact]
        public void Context_Requested_Populates_Spell_Check_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new TestSpellCheckResult(0, 3, "Ths") },
                        new[] { "This", "The" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.NotEmpty(SpellCheck.GetSuggestions(target));
                Assert.Equal(new[] { "This", "The" }, SpellCheck.GetSuggestions(target));
            }
        }

        [Fact]
        public void Context_Requested_Limits_Spell_Check_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new TestSpellCheckResult(0, 3, "Ths") },
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
                    SpellCheck.GetSuggestions(target));
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
                        new TestSpellCheckResult(0, 3, "Ths"),
                        new TestSpellCheckResult(4, 4, "wrng")
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

                SpellCheck.SetIsEnabled(target, true);
                var window = new Window
                {
                    Content = target,
                    Width = 120,
                    Height = 80
                };
                window.Show();
                target.SelectionStart = 0;
                target.SelectionEnd = target.Text!.Length;
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

                Assert.NotEmpty(SpellCheck.GetSuggestions(target));
                Assert.Equal("wrng", provider.LastSuggestedWord);
                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(target.Text!.Length, target.SelectionEnd);

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
                        new[] { new TestSpellCheckResult(0, 3, "Ths") },
                        new[] { "This" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                target.ApplySpellCheckSuggestion("This");

                Assert.Equal("This sample", target.Text);
                Assert.Empty(SpellCheck.GetSuggestions(target));
            }
        }

        [Theory]
        [InlineData(VerticalAlignment.Center, "wrng", "Ths\nwrong\nerrr")]
        [InlineData(VerticalAlignment.Bottom, "errr", "Ths\nwrng\nwrong")]
        public void Pointer_Suggestions_Use_Rendered_Vertical_Offset(
            VerticalAlignment alignment, string expectedWord, string expectedText)
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths"), new TestSpellCheckResult(4, 4, "wrng"), new TestSpellCheckResult(9, 4, "errr") },
                    new[] { "wrong" });
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);
                var target = CreateTextBoxInTopLevel("Ths\nwrng\nerrr");
                var presenter = GetVisualDescendant<TextPresenter>(target);
                presenter.Arrange(new Rect(0, 0, 200, 12));
                presenter.VerticalAlignment = alignment;
                Assert.True(presenter.GetTextVerticalOffset() < 0);

                var root = Assert.IsAssignableFrom<Visual>(presenter.VisualRoot);
                var position = presenter.TranslatePoint(new Point(4, 6), root);
                Assert.NotNull(position);
                presenter.RaiseEvent(CreateContextRequestedAtRootPoint(presenter, root, position.Value));

                Assert.Equal(expectedWord, provider.LastSuggestedWord);
                target.ApplySpellCheckSuggestion("wrong");
                Assert.Equal(expectedText, target.Text);
            }
        }

        [Fact]
        public void Context_Requested_Away_From_Misspelling_Clears_Spell_Check_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new TestSpellCheckResult(0, 3, "Ths") },
                        new[] { "This" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.NotEmpty(SpellCheck.GetSuggestions(target));

                target.CaretIndex = target.Text!.Length;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Empty(SpellCheck.GetSuggestions(target));
                Assert.Empty(SpellCheck.GetSuggestions(target));
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
        public void Spell_Check_Manager_Refreshes_An_Unsupported_Language()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    new[] { "This" },
                    isLanguageSupported: false);

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                // The manager must survive an unsupported answer so a later debounce can refresh it.
                Assert.True(target.HasSpellCheckManager);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal(0, provider.CheckCount);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());

                provider.LanguageSupported = true;
                target.Text = "Ths samples";
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(1, provider.CheckCount);
                Assert.Equal("Ths", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Changing_Language_Invalidates_Cached_Results()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "Ths sample",
                    tb => SpellCheck.SetLanguages(tb, new[] { "en-US" }));
                FireSpellCheckTimer();
                Assert.Equal("Ths", GetDecoratedText(target));

                provider.Results = Array.Empty<ISpellCheckResult>();
                SpellCheck.SetLanguages(target, new[] { "fr-FR" });
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();

                Assert.Equal(2, provider.CheckCount);
                Assert.Empty(GetDecoratedText(target));
                Assert.Contains("Ths", provider.LastCheckedText!);
            }
        }

        [Fact]
        public void Spell_Check_Manager_Is_Not_Created_When_Spell_Check_Is_Disabled()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "Ths sample",
                    textBox => SpellCheck.SetIsEnabled(textBox, false));

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
                    new[] { new TestSpellCheckResult(0, 3, "123") },
                    new[] { "one two three" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "123",
                    textBox =>
                    {
                        TextInputOptions.SetContentType(textBox, TextInputContentType.Number);
                        SpellCheck.SetIsEnabled(textBox, true);
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
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.True(target.HasSpellCheckManager);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests());

                SpellCheck.SetIsEnabled(target, false);

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
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
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
        public void Editing_One_Word_Keeps_Other_Underlines_And_Rechecks_Only_That_Word()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[]
                    {
                        new TestSpellCheckResult(0, 3, "Ths"),
                        new TestSpellCheckResult(0, 4, "wrod"),
                        new TestSpellCheckResult(0, 5, "wrods")
                    },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample wrod here");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Thswrod", GetDecoratedText(target));
                Assert.Equal(1, provider.CheckCount);

                // Editing the second word must leave the first underline visible.
                target.Text = "Ths sample wrods here";

                Assert.Equal("Ths", GetDecoratedText(target));

                // Settle layout and ScrollChanged before firing the debounce timer.
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(2, provider.CheckCount);
                Assert.Equal("wrods", provider.LastCheckedText);
                Assert.Equal("Thswrods", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Inserting_Text_Before_A_Misspelling_Shifts_Its_Underline()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 4, "wrod") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("a wrod");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("wrod", GetDecoratedText(target));

                target.Text = "abc wrod";

                // Shifted, not dropped, and without asking the provider again.
                Assert.Equal("wrod", GetDecoratedText(target));
                var presenter = GetVisualDescendant<TextPresenter>(target);
                Assert.Equal(4, Assert.Single(presenter.SpellCheckRanges!).Start);

                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(2, provider.CheckCount);
                Assert.Equal("abc", provider.LastCheckedText);
            }
        }

        [Fact]
        public void Word_At_Caret_Is_Not_Underlined_While_Focused()
        {
            using (UnitTestApplication.Start(FocusServices.With(renderInterface: new HeadlessPlatformRenderInterface())))
            {
                var provider = new TestSpellCheckProvider(
                    new[]
                    {
                        new TestSpellCheckResult(0, 3, "Ths"),
                        new TestSpellCheckResult(0, 4, "wrod"),
                        new TestSpellCheckResult(0, 5, "wrodx"),
                        new TestSpellCheckResult(0, 4, "qwik")
                    },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths wrod");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Thswrod", GetDecoratedText(target));

                target.Focus();
                target.CaretIndex = 8;

                // Merely placing the caret in an already-flagged word keeps its underline.
                Assert.Equal("Thswrod", GetDecoratedText(target));

                // A programmatic replacement (binding, undo, ...) is not "typing" either.
                target.Text = "Ths wrod qwik";
                target.CaretIndex = 13;
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();
                Assert.Equal("Thswrodqwik", GetDecoratedText(target));

                // Hide the edited word's underline while the caret stays in it.
                target.SelectionStart = target.SelectionEnd = target.CaretIndex = 8;
                RaiseTextEvent(target, "x");
                Assert.Equal("Ths wrodx qwik", target.Text);

                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();

                // The provider reports the complete edited word; it is hidden while typing.
                Assert.Equal("Thsqwik", GetDecoratedText(target));

                // Leaving the word shows the underline.
                target.CaretIndex = 0;

                Assert.Equal("Thswrodxqwik", GetDecoratedText(target));
            }
        }

        [Fact]
        public void First_User_Edit_Is_Suppressed_Until_Caret_Leaves_The_Word()
        {
            using (UnitTestApplication.Start(FocusServices.With(renderInterface: new HeadlessPlatformRenderInterface())))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 4, "wrod") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(string.Empty);
                target.Focus();

                Assert.False(target.HasSpellCheckManager);
                RaiseTextEvent(target, "wrod");

                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();

                Assert.True(target.HasSpellCheckManager);
                Assert.Empty(GetDecoratedText(target));

                // Moving to the exact start leaves the typing range and clears it permanently.
                target.CaretIndex = 0;
                Assert.Equal("wrod", GetDecoratedText(target));
                target.CaretIndex = 1;
                Assert.Equal("wrod", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Multiword_User_Edit_Suppresses_Only_The_Word_At_The_Caret()
        {
            using (UnitTestApplication.Start(FocusServices.With(renderInterface: new HeadlessPlatformRenderInterface())))
            {
                var provider = new TestSpellCheckProvider(
                    new[]
                    {
                        new TestSpellCheckResult(0, 4, "wrod"),
                        new TestSpellCheckResult(0, 6, "anothr")
                    },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(string.Empty);
                target.Focus();
                RaiseTextEvent(target, "wrod anothr");

                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();

                Assert.Equal("wrod", GetDecoratedText(target));

                target.CaretIndex = 0;
                Assert.Equal("wrodanothr", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Programmatic_SelectedText_Replacement_Is_Not_Suppressed()
        {
            using (UnitTestApplication.Start(FocusServices.With(renderInterface: new HeadlessPlatformRenderInterface())))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 4, "wrod") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("good");
                FireSpellCheckTimer();
                target.Focus();
                target.SelectionStart = 0;
                target.SelectionEnd = 4;

                target.SelectedText = "wrod";

                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();

                Assert.Equal("wrod", GetDecoratedText(target));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Pending_Suggestions_Are_Canceled_When_Their_Context_Changes(int changeKind)
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new BlockingSuggestionSpellCheckProvider();
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");
                FireSpellCheckTimer();
                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.False(provider.SuggestionCancellationToken.IsCancellationRequested);

                switch (changeKind)
                {
                    case 0:
                        target.CaretIndex = target.Text!.Length;
                        break;
                    case 1:
                        target.SelectionStart = 4;
                        break;
                    default:
                        target.IsReadOnly = true;
                        break;
                }

                Assert.True(provider.SuggestionCancellationToken.IsCancellationRequested);

                provider.CompleteSuggestions();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Empty(SpellCheck.GetSuggestions(target));
                Assert.Empty(SpellCheck.GetSuggestions(target));
            }
        }

        [Fact]
        public void Provider_Receives_Explicit_Culture_And_Rechecks_When_It_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "Ths sample",
                    tb => SpellCheck.SetLanguages(tb, new[] { "en-US" }));

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.True(provider.HasCheckedCulture);
                Assert.Equal("en-US", provider.LastCheckedCulture?.Name);

                SpellCheck.SetLanguages(target, new[] { "de-DE" });

                Assert.Empty(GetDecoratedText(target));
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal("de-DE", provider.LastCheckedCulture?.Name);
                Assert.Equal("Ths", GetDecoratedText(target));
            }
        }

        [Fact]
        public void System_Culture_Change_Recreates_The_Session_When_No_Culture_Is_Set()
        {
            var platformSettings = new SpellCheckPlatformSettings("en-US");

            using (UnitTestApplication.Start(Services.With(platformSettings: platformSettings)))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");
                FireSpellCheckTimer();

                Assert.Equal("en-US", provider.LastCheckedCulture?.Name);
                Assert.Equal("Ths", GetDecoratedText(target));

                provider.Results = Array.Empty<ISpellCheckResult>();
                platformSettings.SetPreferredApplicationLanguage("fr-FR");

                Assert.Empty(GetDecoratedText(target));
                FireSpellCheckTimer();

                Assert.Equal("fr-FR", provider.LastCheckedCulture?.Name);
                Assert.Equal(2, provider.CheckCount);
            }
        }

        [Fact]
        public void Provider_Exception_Is_Logged_And_Preserves_Unrelated_Results()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Ths", GetDecoratedText(target));

                var messages = new List<string>();
                provider.CheckException = new InvalidOperationException("dictionary missing");

                using (TestLogSink.Start((_, _, _, messageTemplate, _) => messages.Add(messageTemplate)))
                {
                    target.Text = "Ths sampl";
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                    Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                }

                Assert.Contains(messages, m => m.Contains("Spell check provider"));
                Assert.Equal("Ths", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Transient_Provider_Exception_Retries_Without_Caching_A_Clean_Result()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TransientSpellCheckProvider();
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                FireSpellCheckTimer();
                Assert.Equal(1, provider.CheckCount);
                Assert.Empty(GetDecoratedText(target));

                FireSpellCheckTimer();
                Assert.Equal(2, provider.CheckCount);
                Assert.Equal("Ths", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Read_Only_TextBox_Draws_Underlines_But_Offers_No_Suggestions()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample", tb => tb.IsReadOnly = true);

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Ths", GetDecoratedText(target));

                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.Empty(SpellCheck.GetSuggestions(target));
                Assert.Null(provider.LastSuggestedWord);

                target.ApplySpellCheckSuggestion("This");

                Assert.Equal("Ths sample", target.Text);
            }
        }

        [Theory]
        [InlineData(TextInputContentType.Url)]
        [InlineData(TextInputContentType.Email)]
        [InlineData(TextInputContentType.Password)]
        public void Non_Natural_Language_Content_Is_Never_Spell_Checked(TextInputContentType contentType)
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample", tb => TextInputOptions.SetContentType(tb, contentType));

                Assert.False(target.HasSpellCheckManager);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Sensitive_And_Password_Inputs_Are_Never_Spell_Checked()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var sensitive = CreateTextBoxInTopLevel("Ths sample", tb => TextInputOptions.SetIsSensitive(tb, true));
                var password = CreateTextBoxInTopLevel("Ths sample", tb => tb.PasswordChar = '*');

                Assert.False(sensitive.HasSpellCheckManager);
                Assert.False(password.HasSpellCheckManager);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
            }
        }

        [Fact]
        public void Detaching_Releases_The_Spell_Check_Manager_And_Its_Timer()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot("Ths sample");
                SpellCheck.SetProvider(target, provider);

                Assert.True(target.HasSpellCheckManager);
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests());

                topLevel.Content = null;

                // Detachment must prevent manager recreation, even with a local provider.
                target.Text = "Ths samples";

                Assert.False(target.HasSpellCheckManager);
                Assert.Empty(Dispatcher.SnapshotTimersForUnitTests());
                Assert.Equal(0, provider.CheckCount);
            }
        }

        [Theory]
        [InlineData(typeof(NumericUpDown))]
        [InlineData(typeof(CalendarDatePicker))]
        public void Inner_TextBox_Is_Not_Spell_Checked_When_Every_TextBox_Opts_In(Type controlType)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var theme = new SimpleTheme();
                var target = (TemplatedControl)Activator.CreateInstance(controlType)!;
                var plain = new TextBox();
                var root = new TestRoot
                {
                    Styles =
                    {
                        theme,
                        new Style(x => x.OfType<TextBox>())
                        {
                            Setters = { new Setter(SpellCheck.IsEnabledProperty, true) }
                        }
                    },
                    Child = new StackPanel { Children = { target, plain } }
                };
                SpellCheck.SetIsEnabled(root, true);

                Assert.True(theme.TryGetResource(controlType, ThemeVariant.Default, out var controlTheme));
                target.Theme = Assert.IsType<ControlTheme>(controlTheme);
                root.LayoutManager.ExecuteInitialLayoutPass();

                var inner = target.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "PART_TextBox");
                Assert.True(SpellCheck.GetIsEnabled(plain));
                Assert.False(SpellCheck.GetIsEnabled(inner));
            }
        }

        [Fact]
        public void MaskedTextBox_With_A_Mask_Is_Never_Spell_Checked()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(Array.Empty<ISpellCheckResult>(), Array.Empty<string>());
                var masked = new MaskedTextBox { Mask = "LLL-000", Text = "abc-123" };
                var unmasked = new MaskedTextBox { Text = "abc" };

                foreach (var box in new[] { masked, unmasked })
                {
                    SpellCheck.SetIsEnabled(box, true);
                    SpellCheck.SetProvider(box, provider);
                }

                Assert.False(TextBoxSpellCheckManager.CanCreate(masked));
                Assert.True(TextBoxSpellCheckManager.CanCreate(unmasked));
            }
        }

        [Fact]
        public void Spell_Check_Results_Are_Cleared_When_Provider_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");

                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                Assert.Equal("Ths", GetDecoratedText(target));

                SpellCheck.SetProvider(
                    target,
                    new TestSpellCheckProvider(Array.Empty<ISpellCheckResult>(), Array.Empty<string>()));

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
        public void Finishing_A_Check_After_Scrolling_Back_Keeps_The_Visible_Underlines()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new GatedSpellCheckProvider();
                var text = CreateMisspelledLines(40, out _);

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    text,
                    textBox =>
                    {
                        textBox.Width = 200;
                        textBox.Height = 60;
                    });

                FireAllSpellCheckTimers();
                Assert.StartsWith("wrng00", GetDecoratedText(target));

                provider.IsBlocking = true;
                var scrollViewer = GetVisualDescendant<ScrollViewer>(target);
                scrollViewer.Offset = scrollViewer.Offset.WithY(scrollViewer.ScrollBarMaximum.Y);
                topLevel.LayoutManager.ExecuteLayoutPass();
                FireAllSpellCheckTimers();
                Assert.True(provider.HasPendingCheck);

                scrollViewer.Offset = scrollViewer.Offset.WithY(0);
                topLevel.LayoutManager.ExecuteLayoutPass();
                Assert.StartsWith("wrng00", GetDecoratedText(target));

                provider.Release();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.StartsWith("wrng00", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Suggestions_That_Exceed_MaxLength_Are_Not_Offered_Or_Applied()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 4, "alot") },
                    new[] { "a lot", "alto" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("alot here", tb => tb.MaxLength = 9);
                FireAllSpellCheckTimers();

                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "alto" }, SpellCheck.GetSuggestions(target));

                target.ApplySpellCheckSuggestion("a lot");

                Assert.Equal("alot here", target.Text);
            }
        }

        [Fact]
        public void Right_Click_Outside_The_Text_Offers_No_Suggestions()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(4, 4, "wrng") },
                    new[] { "wrong" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "Ths wrng",
                    Width = 300,
                    Height = 32
                };

                SpellCheck.SetIsEnabled(target, true);
                var window = new Window { Content = target, Width = 340, Height = 80 };
                window.Show();
                var presenter = GetVisualDescendant<TextPresenter>(target);
                var root = Assert.IsAssignableFrom<Visual>(presenter.VisualRoot);
                var y = presenter.TextLayout.Height / 2;

                var outside = presenter.TranslatePoint(new Point(presenter.TextLayout.Width + 60, y), root);
                Assert.NotNull(outside);
                presenter.RaiseEvent(CreateContextRequestedAtRootPoint(presenter, root, outside.Value));
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Empty(SpellCheck.GetSuggestions(target));

                var inside = presenter.TranslatePoint(new Point(presenter.TextLayout.Width - 1, y), root);
                Assert.NotNull(inside);
                presenter.RaiseEvent(CreateContextRequestedAtRootPoint(presenter, root, inside.Value));
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "wrong" }, SpellCheck.GetSuggestions(target));
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
        public void Replacing_Presenter_Checks_Newly_Visible_Text()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 4, "wrng") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    "wrng one\nwrng two\nwrng three",
                    textBox =>
                    {
                        textBox.Width = 200;
                        textBox.Height = 20;
                    });

                FireSpellCheckTimer();

                var oldPresenter = GetVisualDescendant<TextPresenter>(target);
                Assert.Equal(1, provider.CheckCount);
                Assert.DoesNotContain("wrng three", provider.LastCheckedText!);

                target.Template = CreateTemplate();
                target.Height = 100;
                topLevel.LayoutManager.ExecuteLayoutPass();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.NotSame(oldPresenter, GetVisualDescendant<TextPresenter>(target));
                FireSpellCheckTimer();

                Assert.Equal(2, provider.CheckCount);
                Assert.Contains("wrng three", provider.LastCheckedText!);
                Assert.Equal("wrngwrngwrng", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Replacement_Presenter_Continues_To_Observe_Viewport_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                IControlTemplate PresenterTemplate() => new FuncControlTemplate<TextBox>((control, scope) =>
                    new TextPresenter
                    {
                        Name = "PART_TextPresenter",
                        [!TextPresenter.TextProperty] = control[!TextBox.TextProperty],
                    }.RegisterInNameScope(scope));

                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 4, "wrng") },
                    Array.Empty<string>());
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    "wrng one\nwrng two\nwrng three",
                    textBox =>
                    {
                        // Without a ScrollViewer, resizing is reported by the presenter's Bounds.
                        textBox.Template = PresenterTemplate();
                        textBox.Width = 200;
                        textBox.Height = 20;
                    });

                FireSpellCheckTimer();

                target.Template = PresenterTemplate();
                topLevel.LayoutManager.ExecuteLayoutPass();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                FireSpellCheckTimer();

                // The replacement initially exposes only cached text.
                Assert.Equal(1, provider.CheckCount);
                Assert.Equal("wrng", GetDecoratedText(target));

                target.Height = 100;
                topLevel.LayoutManager.ExecuteLayoutPass();
                FireSpellCheckTimer();

                Assert.Equal(2, provider.CheckCount);
                Assert.Equal("wrngwrngwrng", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Large_Text_Checks_Visible_Text_Only()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    Array.Empty<ISpellCheckResult>(),
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
                    new[] { new TestSpellCheckResult(0, int.MaxValue) },
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

                // Never send incomplete tokens to the provider.
                Assert.Equal(0, provider.CheckCount);
                Assert.Null(provider.LastCheckedText);
                Assert.Empty(GetDecoratedText(target));
            }
        }

        [Fact]
        public void Context_Requested_Checks_Large_Text_Context_Only()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    new[] { "This" });

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths " + new string('a', 10_001));
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(1, provider.CheckCount);
                Assert.Equal("Ths", provider.LastCheckedText);
                Assert.True(target.HasSpellCheckManager);
                Assert.NotEmpty(SpellCheck.GetSuggestions(target));
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
                        new TestSpellCheckResult(0, 3, "Ths"),
                        new TestSpellCheckResult(4, 4, "wrng")
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
                        new TestSpellCheckResult(4, 4, "wrng"),
                        new TestSpellCheckResult(0, 3, "Ths")
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
        public void Suggestion_Query_Waits_For_A_Pending_Background_Check_Instead_Of_Canceling_It()
        {
            using (UnitTestApplication.Start(Services))
            {
                // Real asynchronous completion must resume on the UI thread, as it does in an application.
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());

                var provider = new QueuedSpellCheckProvider();

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel("Ths sample");
                Assert.Single(Dispatcher.SnapshotTimersForUnitTests()).ForceFire();
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var background = Assert.Single(provider.Checks);

                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                // The context runs one call at a time, so the suggestion check is queued behind the background check.
                Assert.Single(provider.Checks);
                Assert.False(background.CancellationToken.IsCancellationRequested);

                background.Complete(new TestSpellCheckResult(0, 3, "Ths", new[] { "This" }));
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal("Ths", GetDecoratedText(target));

                var queued = Assert.Single(provider.Checks, c => c != background);
                queued.Complete(new TestSpellCheckResult(0, 3, "Ths", new[] { "This" }));
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "This" }, SpellCheck.GetSuggestions(target));
                Assert.True(SpellCheck.GetHasSuggestions(target));
                Assert.Equal("Ths", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Locale_Hints_Are_Used_When_No_Spell_Check_Language_Is_Set()
        {
            using (UnitTestApplication.Start(Services.With(platformSettings: new SpellCheckPlatformSettings("en-US"))))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);

                var target = CreateTextBoxInTopLevel(
                    "Ths sample",
                    t => TextInputOptions.SetLocaleHints(t, new[] { "de-DE" }));
                FireSpellCheckTimer();

                Assert.Equal("de-DE", provider.LastCheckedCulture?.Name);

                SpellCheck.SetLanguages(target, new[] { "fr-FR" });
                FireSpellCheckTimer();

                Assert.Equal("fr-FR", provider.LastCheckedCulture?.Name);

                SpellCheck.SetLanguages(target, null);
                TextInputOptions.SetLocaleHints(target, null);
                FireSpellCheckTimer();

                Assert.Equal("en-US", provider.LastCheckedCulture?.Name);
            }
        }

        [Fact]
        public void Spell_Check_Context_Is_Disposed_When_Culture_Provider_Or_Attachment_Changes()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TrackingSpellCheckProvider();
                var (target, topLevel) = CreateTextBoxInTopLevelWithRoot(
                    "Ths sample",
                    t => SpellCheck.SetProvider(t, provider));
                FireSpellCheckTimer();

                var first = Assert.Single(provider.Contexts);
                Assert.False(first.IsDisposed);

                SpellCheck.SetLanguages(target, new[] { "fr-FR" });
                FireSpellCheckTimer();

                Assert.True(first.IsDisposed);
                Assert.Equal(2, provider.Contexts.Count);
                var second = provider.Contexts[1];

                var otherProvider = new TrackingSpellCheckProvider();
                SpellCheck.SetProvider(target, otherProvider);
                FireSpellCheckTimer();

                Assert.True(second.IsDisposed);
                var third = Assert.Single(otherProvider.Contexts);

                topLevel.Content = null;

                Assert.True(third.IsDisposed);
                Assert.All(provider.Contexts, c => Assert.Equal(1, c.DisposeCount));
                Assert.Equal(1, third.DisposeCount);
            }
        }

        [Fact]
        public void Applying_A_Suggestion_Is_A_Single_Undo_Step()
        {
            using (UnitTestApplication.Start(Services))
            {
                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(
                    new TestSpellCheckProvider(
                        new[] { new TestSpellCheckResult(0, 3, "Ths") },
                        new[] { "This" }));

                var target = CreateTextBoxInTopLevel("Ths sample");
                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                target.ApplySpellCheckSuggestion("This");
                Assert.Equal("This sample", target.Text);

                target.Undo();
                Assert.Equal("Ths sample", target.Text);
            }
        }

        [Fact]
        public void Unresolvable_Spell_Check_Language_Turns_Checking_Off_With_One_Warning()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>());

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);
                var messages = new List<string>();
                var values = new List<object?>();

                using (TestLogSink.Start((_, _, _, template, args) =>
                {
                    messages.Add(template);
                    values.AddRange(args);
                }))
                {
                    var target = CreateTextBoxInTopLevel("Ths sample", tb => SpellCheck.SetLanguages(tb, new[] { "not a language tag" }));
                    target.Text = "Ths samples";
                    target.Text = "Ths sampless";
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                    Assert.False(target.HasSpellCheckManager);
                    Assert.Equal(0, provider.CheckCount);
                    Assert.Single(messages, m => m.Contains("cannot be resolved"));
                    Assert.Contains("not a language tag", values);
                }
            }
        }

        [Fact]
        public void Missing_Dictionary_Is_Logged_Once()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new TestSpellCheckProvider(
                    new[] { new TestSpellCheckResult(0, 3, "Ths") },
                    Array.Empty<string>(),
                    isLanguageSupported: false);

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(provider);
                var messages = new List<string>();

                using (TestLogSink.Start((_, _, _, template, _) => messages.Add(template)))
                {
                    var target = CreateTextBoxInTopLevel("Ths sample", tb => SpellCheck.SetLanguages(tb, new[] { "kl-GL" }));
                    FireSpellCheckTimer();
                    target.Text = "Ths samples";
                    FireSpellCheckTimer();

                    Assert.Equal(0, provider.CheckCount);
                    Assert.Single(messages, m => m.Contains("has no dictionary"));
                }
            }
        }

        [Fact]
        public void Each_TextBox_Checks_In_Its_Own_Language()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var english = new TextBox { Template = CreateTemplate(), Text = "This frase" };
                var spanish = new TextBox { Template = CreateTemplate(), Text = "This frase" };

                foreach (var (box, language) in new[] { (english, "en-US"), (spanish, "es-ES") })
                {
                    SpellCheck.SetIsEnabled(box, true);
                    SpellCheck.SetProvider(box, provider);
                    SpellCheck.SetLanguages(box, new[] { language });
                }

                var topLevel = new TestTopLevel(CreateMockTopLevelImpl().Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = new StackPanel { Children = { english, spanish } }
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();
                FireAllSpellCheckTimers();

                Assert.Equal(new[] { "en-US", "es-ES" }, provider.CreatedCultures);
                Assert.Equal("frase", GetDecoratedText(english));
                Assert.Equal("This", GetDecoratedText(spanish));
            }
        }

        [Fact]
        public void Several_Languages_Report_Only_Words_Wrong_In_All_Of_Them()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var target = CreateTextBoxInTopLevel("This frase has eror", tb =>
                {
                    SpellCheck.SetProvider(tb, provider);
                    SpellCheck.SetLanguages(tb, new[] { "en-US", "es-ES" });
                });
                FireAllSpellCheckTimers();

                Assert.Equal(new[] { "en-US", "es-ES" }, provider.CreatedCultures);
                Assert.Equal("eror", GetDecoratedText(target));

                target.CaretIndex = target.Text!.IndexOf("eror", StringComparison.Ordinal) + 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                // Suggestions from both languages are interleaved without duplicates.
                Assert.Equal(new[] { "error", "eros", "erosión" }, SpellCheck.GetSuggestions(target));
            }
        }

        [Fact]
        public void Unresolvable_Or_Unsupported_Languages_In_A_List_Are_Skipped()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var messages = new List<string>();

                using (TestLogSink.Start((_, _, _, template, _) => messages.Add(template)))
                {
                    var target = CreateTextBoxInTopLevel("This frase", tb =>
                    {
                        SpellCheck.SetProvider(tb, provider);
                        SpellCheck.SetLanguages(tb, new[] { "not a language tag either", "en-US", "fr-FR" });
                    });
                    FireAllSpellCheckTimers();

                    Assert.Equal(new[] { "en-US", "fr-FR" }, provider.RequestedCultures);
                    Assert.Equal(new[] { "en-US" }, provider.CreatedCultures);
                    Assert.Equal("frase", GetDecoratedText(target));
                    Assert.Contains(messages, m => m.Contains("is skipped"));
                }
            }
        }

        [Fact]
        public void Only_The_First_Valid_Locale_Hint_Is_Used()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var target = CreateTextBoxInTopLevel("This frase", tb =>
                {
                    SpellCheck.SetProvider(tb, provider);
                    TextInputOptions.SetLocaleHints(tb, new[] { "not a language tag", "es-ES", "en-US" });
                });
                FireAllSpellCheckTimers();

                Assert.Equal(new[] { "es-ES" }, provider.CreatedCultures);
                Assert.Equal("This", GetDecoratedText(target));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void Languages_Without_Tags_Behave_As_Unset(int blankEntries)
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var target = CreateTextBoxInTopLevel("This frase", tb =>
                {
                    SpellCheck.SetProvider(tb, provider);
                    SpellCheck.SetLanguages(tb, blankEntries == 0 ? Array.Empty<string>() : new[] { " ", "" });
                    TextInputOptions.SetLocaleHints(tb, new[] { "es-ES" });
                });
                FireAllSpellCheckTimers();

                Assert.Equal(new[] { "es-ES" }, provider.CreatedCultures);
                Assert.Equal("This", GetDecoratedText(target));
            }
        }

        [Fact]
        public void Language_Contexts_Are_Reused_And_Missing_Languages_Are_Not_Requested_Again()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var target = CreateTextBoxInTopLevel("This frase has eror", tb =>
                {
                    SpellCheck.SetProvider(tb, provider);
                    SpellCheck.SetLanguages(tb, new[] { "en-US", "es-ES", "fr-FR" });
                });
                FireAllSpellCheckTimers();

                for (var i = 0; i < 3; i++)
                {
                    target.Text += " eror";
                    FireAllSpellCheckTimers();
                }

                Assert.Equal(new[] { "en-US", "es-ES", "fr-FR" }, provider.RequestedCultures);
                Assert.Equal(new[] { "en-US", "es-ES" }, provider.CreatedCultures);
                Assert.Empty(provider.DisposedCultures);
            }
        }

        [Fact]
        public void Changing_Languages_Disposes_Every_Previous_Context()
        {
            using (UnitTestApplication.Start(Services))
            {
                var provider = new DictionarySpellCheckProvider();
                var target = CreateTextBoxInTopLevel("This frase has eror", tb =>
                {
                    SpellCheck.SetProvider(tb, provider);
                    SpellCheck.SetLanguages(tb, new[] { "en-US", "es-ES" });
                });
                FireAllSpellCheckTimers();

                SpellCheck.SetLanguages(target, new[] { "es-ES" });
                FireAllSpellCheckTimers();

                Assert.Equal(new[] { "en-US", "es-ES" }, provider.DisposedCultures);
                Assert.Equal(new[] { "en-US", "es-ES", "es-ES" }, provider.CreatedCultures);
                Assert.Equal("Thiseror", GetDecoratedText(target));
            }
        }

        private static void FireAllSpellCheckTimers()
        {
            foreach (var timer in Dispatcher.SnapshotTimersForUnitTests())
            {
                if (timer.Interval == TimeSpan.FromMilliseconds(250))
                {
                    timer.ForceFire();
                }
            }

            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
        }

        // Each language accepts its own words and suggests from a small table.
        private sealed class DictionarySpellCheckProvider : ISpellCheckProvider
        {
            private static readonly Dictionary<string, string[]> s_words = new()
            {
                ["en-US"] = new[] { "This", "has" },
                ["es-ES"] = new[] { "frase", "has" }
            };

            private static readonly Dictionary<string, string[]> s_suggestions = new()
            {
                ["en-US"] = new[] { "error", "eros" },
                ["es-ES"] = new[] { "error", "erosión" }
            };

            public List<string> RequestedCultures { get; } = new();

            public List<string> CreatedCultures { get; } = new();

            public List<string> DisposedCultures { get; } = new();

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext? CreateContext(CultureInfo culture)
            {
                RequestedCultures.Add(culture.Name);

                if (!s_words.TryGetValue(culture.Name, out var words))
                {
                    return null;
                }

                CreatedCultures.Add(culture.Name);
                return new Context(words, s_suggestions[culture.Name], () => DisposedCultures.Add(culture.Name));
            }

            private sealed class Context(string[] words, string[] suggestions, Action disposed) : SpellCheckContextBase
            {
                protected override void DisposeCore() => disposed();

                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken)
                {
                    var value = text.ToString();
                    var results = new List<ISpellCheckResult>();
                    var start = 0;

                    foreach (var word in value.Split(' '))
                    {
                        if (word.Length > 0 && Array.IndexOf(words, word) < 0)
                        {
                            results.Add(new TestSpellCheckResult(start, word.Length, word, suggestions));
                        }

                        start += word.Length + 1;
                    }

                    return new ValueTask<IReadOnlyList<ISpellCheckResult>>(results);
                }
            }
        }

        private sealed class QueuedSpellCheckProvider : ISpellCheckProvider
        {
            public List<PendingCheck> Checks { get; } = new();

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture) => new Context(this);

            private sealed class Context(QueuedSpellCheckProvider owner) : SpellCheckContextBase
            {
                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken)
                {
                    var pending = new PendingCheck(cancellationToken);
                    owner.Checks.Add(pending);
                    return new ValueTask<IReadOnlyList<ISpellCheckResult>>(pending.Task);
                }
            }

            public sealed class PendingCheck(CancellationToken cancellationToken)
            {
                private readonly TaskCompletionSource<IReadOnlyList<ISpellCheckResult>> _completion = new();

                public CancellationToken CancellationToken { get; } = cancellationToken;

                public Task<IReadOnlyList<ISpellCheckResult>> Task => _completion.Task;

                public void Complete(params ISpellCheckResult[] results) => _completion.SetResult(results);
            }
        }

        private sealed class TrackingSpellCheckProvider : ISpellCheckProvider
        {
            public List<Context> Contexts { get; } = new();

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture)
            {
                var context = new Context();
                Contexts.Add(context);
                return context;
            }

            public sealed class Context : SpellCheckContextBase
            {
                public int DisposeCount { get; private set; }

                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken) =>
                    new(Array.Empty<ISpellCheckResult>());

                protected override void DisposeCore() => DisposeCount++;
            }
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

            SpellCheck.SetIsEnabled(target, true);

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

        private static string CreateMisspelledLines(int count, out IReadOnlyList<ISpellCheckResult> results)
        {
            var text = new StringBuilder();
            var spellCheckResults = new List<ISpellCheckResult>(count);

            for (var i = 0; i < count; i++)
            {
                var word = FormattableString.Invariant($"wrng{i:00}");

                spellCheckResults.Add(new TestSpellCheckResult(text.Length, word.Length, word));
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
            var text = textBox.Text ?? string.Empty;

            if (presenter.SpellCheckRanges is { } ranges)
            {
                foreach (var range in ranges)
                {
                    if (range.Start >= 0 && range.Start + range.Length <= text.Length)
                    {
                        decoratedText.Append(text, range.Start, range.Length);
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

        // Select by interval to avoid firing the caret-blink timer.
        private static void FireSpellCheckTimer()
        {
            Assert.Single(Dispatcher.SnapshotTimersForUnitTests(), t => t.Interval == TimeSpan.FromMilliseconds(250)).ForceFire();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
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

        // Reports every "wrngNN" word, and can hold a check until it is released.
        private sealed class GatedSpellCheckProvider : ISpellCheckProvider
        {
            private TaskCompletionSource<bool>? _gate;

            public bool IsBlocking { get; set; }

            public bool HasPendingCheck => _gate is { Task.IsCompleted: false };

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture) => new Context(this);

            public void Release() => _gate?.TrySetResult(true);

            private sealed class Context(GatedSpellCheckProvider owner) : ISpellCheckContext
            {
                public async ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken = default)
                {
                    if (owner.IsBlocking)
                    {
                        owner._gate = new TaskCompletionSource<bool>();
                        await owner._gate.Task;
                    }

                    var value = text.ToString();
                    var results = new List<ISpellCheckResult>();

                    for (var index = value.IndexOf("wrng", StringComparison.Ordinal);
                         index >= 0 && index + 6 <= value.Length;
                         index = value.IndexOf("wrng", index + 1, StringComparison.Ordinal))
                    {
                        results.Add(new TestSpellCheckResult(index, 6, value.Substring(index, 6)));
                    }

                    return results;
                }

                public void Dispose()
                {
                }
            }
        }

        private sealed class SpellCheckPlatformSettings(string language) : DefaultPlatformSettings
        {
            private string _language = language;

            public override string PreferredApplicationLanguage => _language;

            public void SetPreferredApplicationLanguage(string value)
            {
                _language = value;
                OnPreferredApplicationLanguageChanged();
            }
        }

        private class TestSpellCheckProvider : ISpellCheckProvider
        {
            private readonly IReadOnlyList<string> _suggestions;

            public TestSpellCheckProvider(
                IReadOnlyList<ISpellCheckResult> results,
                IReadOnlyList<string> suggestions,
                bool isLanguageSupported = true)
            {
                Results = results;
                _suggestions = suggestions;
                LanguageSupported = isLanguageSupported;
            }

            public int CheckCount { get; private set; }

            public string? LastCheckedText { get; private set; }

            public string? LastSuggestedWord { get; private set; }

            public CultureInfo? LastCheckedCulture { get; private set; }

            public bool HasCheckedCulture { get; private set; }

            public Exception? CheckException { get; set; }

            public bool LanguageSupported { get; set; }

            public IReadOnlyList<ISpellCheckResult> Results { get; set; }

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext? CreateContext(CultureInfo culture)
            {
                LastCheckedCulture = culture;
                HasCheckedCulture = true;
                return LanguageSupported ? new Session(this) : null;
            }

            private ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
                string word,
                CancellationToken cancellationToken)
            {
                LastSuggestedWord = word;
                return new ValueTask<IReadOnlyList<string>>(_suggestions);
            }

            private sealed class Session : SpellCheckContextBase
            {
                private readonly TestSpellCheckProvider _owner;

                public Session(TestSpellCheckProvider owner)
                {
                    _owner = owner;
                }

                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> textMemory,
                    CancellationToken cancellationToken)
                {
                    var text = textMemory.Span;
                    _owner.CheckCount++;
                    var textValue = text.ToString();
                    _owner.LastCheckedText = textValue;

                    if (_owner.CheckException is { } exception)
                    {
                        throw exception;
                    }

                    if (_owner.Results.Count == 0)
                    {
                        return new ValueTask<IReadOnlyList<ISpellCheckResult>>(Array.Empty<ISpellCheckResult>());
                    }

                    List<ISpellCheckResult>? results = null;

                    for (var i = 0; i < _owner.Results.Count; i++)
                    {
                        var result = _owner.Results[i];
                        var testResult = result as TestSpellCheckResult;

                        if (testResult?.Word is { Length: > 0 } word)
                        {
                            var searchStart = 0;

                            while (searchStart < textValue.Length)
                            {
                                var start = textValue.IndexOf(word, searchStart, StringComparison.Ordinal);

                                if (start < 0)
                                {
                                    break;
                                }

                                var end = start + word.Length;

                                if (!SpellCheckResultCache.IsWordCharBefore(textValue, start) &&
                                    !SpellCheckResultCache.IsWordCharAt(textValue, end))
                                {
                                    results ??= new List<ISpellCheckResult>();
                                    results.Add(new ProviderResult(this, start, word.Length, word));
                                }

                                searchStart = end;
                            }
                        }
                        else if (result.Start >= 0 && result.Start < textValue.Length)
                        {
                            results ??= new List<ISpellCheckResult>();
                            var length = result.Length;
                            var checkedWord = length <= textValue.Length - result.Start
                                ? textValue.Substring(result.Start, length)
                                : string.Empty;
                            results.Add(new ProviderResult(this, result.Start, length, checkedWord));
                        }
                    }

                    return new ValueTask<IReadOnlyList<ISpellCheckResult>>(
                        results is null || results.Count == 0 ? Array.Empty<ISpellCheckResult>() : results);
                }

                public ValueTask<IReadOnlyList<string>> SuggestWordAsync(
                    string word,
                    CancellationToken cancellationToken) =>
                    _owner.SuggestCoreAsync(word, cancellationToken);
            }

            private sealed class ProviderResult : SpellCheckResultBase
            {
                private readonly Session _session;
                private readonly string _word;

                public ProviderResult(Session session, int start, int length, string word)
                    : base(session, start, length)
                {
                    _session = session;
                    _word = word;
                }

                protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
                    CancellationToken cancellationToken) =>
                    _session.SuggestWordAsync(_word, cancellationToken);
            }
        }

        private class BlockingSpellCheckProvider : ISpellCheckProvider
        {
            private readonly TaskCompletionSource<IReadOnlyList<ISpellCheckResult>> _checkCompletion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public CancellationToken CheckCancellationToken { get; private set; }

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture) => new Session(this);

            private sealed class Session(BlockingSpellCheckProvider owner) : SpellCheckContextBase
            {
                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> textMemory,
                    CancellationToken cancellationToken)
                {
                    owner.CheckCancellationToken = cancellationToken;
                    return new ValueTask<IReadOnlyList<ISpellCheckResult>>(owner._checkCompletion.Task);
                }
            }

            public void Complete()
            {
                _checkCompletion.SetResult(Array.Empty<ISpellCheckResult>());
            }
        }

        private sealed class BlockingSuggestionSpellCheckProvider : ISpellCheckProvider
        {
            private readonly TaskCompletionSource<IReadOnlyList<string>> _suggestionCompletion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public CancellationToken SuggestionCancellationToken { get; private set; }

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture) => new Session(this);

            private sealed class Session(BlockingSuggestionSpellCheckProvider owner) : SpellCheckContextBase
            {
                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken) =>
                    new(new[] { new Result(this) });

                public ValueTask<IReadOnlyList<string>> SuggestBlockingAsync(CancellationToken token)
                {
                    owner.SuggestionCancellationToken = token;
                    return new ValueTask<IReadOnlyList<string>>(owner._suggestionCompletion.Task);
                }
            }

            private sealed class Result(Session session) : SpellCheckResultBase(session, 0, 3)
            {
                protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
                    CancellationToken cancellationToken) =>
                    session.SuggestBlockingAsync(cancellationToken);
            }

            public void CompleteSuggestions()
            {
                _suggestionCompletion.SetResult(new[] { "This" });
            }
        }

        private sealed class TransientSpellCheckProvider : ISpellCheckProvider
        {
            public int CheckCount { get; private set; }

            public IReadOnlyList<CultureInfo> AvailableCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture) => new Session(this);

            private sealed class Session(TransientSpellCheckProvider owner) : SpellCheckContextBase
            {
                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken)
                {
                    owner.CheckCount++;

                    if (owner.CheckCount == 1)
                    {
                        throw new InvalidOperationException("temporarily unavailable");
                    }

                    return new ValueTask<IReadOnlyList<ISpellCheckResult>>(
                        new[] { new TestSpellCheckResult(0, 3, "Ths") });
                }
            }
        }
    }
}
