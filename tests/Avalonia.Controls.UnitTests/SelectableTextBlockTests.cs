using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SelectableTextBlockTests : ScopedTestBase
    {
        // Content: Run("foo") + InlineUIContainer + Run("bar")
        // Inlines.Text after fix: "foo\uFFFCbar" (indices 0-6)
        //   0='f', 1='o', 2='o', 3='\uFFFC' (embedded control), 4='b', 5='a', 6='r'
        [Theory]
        // Entirely before InlineUIContainer
        [InlineData(0, 3, "foo")]
        // Exactly the InlineUIContainer character
        [InlineData(3, 4, "\uFFFC")]
        // Up to and including InlineUIContainer (fencepost: last char before "bar")
        [InlineData(0, 4, "foo\uFFFC")]
        // Starting exactly after InlineUIContainer (fencepost: first char of "bar")
        [InlineData(4, 7, "bar")]
        // InlineUIContainer through end
        [InlineData(3, 7, "\uFFFCbar")]
        // Spanning InlineUIContainer (one char either side)
        [InlineData(2, 5, "o\uFFFCb")]
        // Entire content
        [InlineData(0, 7, "foo\uFFFCbar")]
        public void Selection_With_InlineUIContainer_Returns_Correct_Text(int start, int end, string expected)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock();

                target.Inlines!.Add(new Run("foo"));
                target.Inlines!.Add(new InlineUIContainer(new Border()));
                target.Inlines!.Add(new Run("bar"));

                target.Measure(Size.Infinity);

                // SelectionStart/End values correspond to TextLayout character positions.
                // EmbeddedControlRun occupies 1 position (TextRun.DefaultTextSourceLength),
                // and Inlines.Text now has a matching U+FFFC placeholder, so they stay in sync.
                target.SelectionStart = start;
                target.SelectionEnd = end;

                Assert.Equal(expected, target.SelectedText);
            }
        }

        [Theory]
        [InlineData(TextAlignment.Center)]
        [InlineData(TextAlignment.Right)]
        public void Dragging_Selection_Should_Reach_End_Of_Text_When_Text_Is_Aligned(TextAlignment textAlignment)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    Width = 200,
                    Text = "Aligned text",
                    TextAlignment = textAlignment
                };

                var root = new TestRoot(target)
                {
                    ClientSize = new Size(300, 100)
                };

                root.Measure(root.ClientSize);
                root.Arrange(new Rect(root.ClientSize));
                root.ExecuteInitialLayoutPass();

                var firstCharacterBounds = target.TextLayout.HitTestTextPosition(0);
                var lastCharacterBounds = target.TextLayout.HitTestTextPosition(target.Text!.Length - 1);
                var mouse = new MouseTestHelper();
                var startPoint = new Point(
                    firstCharacterBounds.X + firstCharacterBounds.Width / 2,
                    firstCharacterBounds.Y + firstCharacterBounds.Height / 2);
                var endPoint = new Point(
                    Math.Min(target.Bounds.Width - 1, lastCharacterBounds.Right + 10),
                    lastCharacterBounds.Y + lastCharacterBounds.Height / 2);

                mouse.Down(target, position: target.TranslatePoint(startPoint, root));
                mouse.Move(target, position: target.TranslatePoint(endPoint, root).GetValueOrDefault());

                Assert.Equal(target.Text!.Length, Math.Max(target.SelectionStart, target.SelectionEnd));
            }
        }

        [Fact]
        public void SelectionForeground_Should_Not_Reset_Run_Typeface_And_Style()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    SelectionForegroundBrush = Brushes.Red
                };

                var run = new Run("Hello")
                {
                    FontWeight = FontWeight.Bold,
                    FontStyle = FontStyle.Italic,
                    FontSize = 20
                };

                target.Inlines!.Add(run);

                target.Measure(Size.Infinity);

                target.SelectionStart = 0;
                target.SelectionEnd = run.Text!.Length;

                target.Measure(Size.Infinity);

                var textLayout = target.TextLayout;
                Assert.NotNull(textLayout);

                var textRuns = textLayout.TextLines
                    .SelectMany(l => l.TextRuns)
                    .OfType<ShapedTextRun>()
                    .ToList();

                Assert.NotEmpty(textRuns);

                var selectedRun = textRuns[0];
                var props = selectedRun.Properties;

                Assert.Equal(FontWeight.Bold, props.Typeface.Weight);
                Assert.Equal(FontStyle.Italic, props.Typeface.Style);

                Assert.Same(target.SelectionForegroundBrush, props.ForegroundBrush);
            }
        }

        [Fact]
        public async Task Pointer_Selection_Is_Published_To_Primary_Selection()
        {
            using (UnitTestApplication.Start(TextBoxTests.CreatePrimarySelectionServices()))
            {
                var target = new SelectableTextBlock { Text = "0123" };
                var window = new Window { Content = target };
                window.Show();

                var mouse = new MouseTestHelper();
                mouse.Down(target, MouseButton.Left, new Point(1, 300));
                mouse.Move(target, new Point(700, 300));
                mouse.Up(target, MouseButton.Left, new Point(700, 300));

                Assert.Equal("0123", target.SelectedText);
                Assert.Equal("0123", await window.PrimarySelection!.TryGetTextAsync());
            }
        }

        [Fact]
        public void Inlines_Changes_Should_Update_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock();
                target.Inlines!.Add(new Run("foo"));
                target.SelectionEnd = 3;

                var selectedTextChanged = false;
                target.PropertyChanged += (_, e) =>
                {
                    if (e.Property == SelectableTextBlock.SelectedTextProperty)
                    {
                        selectedTextChanged = true;
                    }
                };

                target.Inlines.Add(new Run("bar"));

                Assert.True(selectedTextChanged);

                target.SelectionStart = 6;
                target.SelectionEnd = 0;
                target.Inlines.RemoveAt(1);

                Assert.Equal(3, target.SelectionStart);
                Assert.Equal(0, target.SelectionEnd);

                target.SelectionStart = 0;
                target.SelectionEnd = 3;

                target.Inlines[0] = new Run("a");

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(1, target.SelectionEnd);
                Assert.Equal("a", target.SelectedText);
            }
        }

        [Fact]
        public void Text_Changes_Should_Update_Selection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    Text = "foo",
                    SelectionEnd = 3
                };

                var selectedTextChanged = false;
                target.PropertyChanged += (_, e) =>
                {
                    if (e.Property == SelectableTextBlock.SelectedTextProperty)
                    {
                        selectedTextChanged = true;
                    }
                };

                target.Text = "a";

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(1, target.SelectionEnd);
                Assert.True(selectedTextChanged);
            }
        }

        [Fact]
        public void CoerceCaretIndex_OnTextChanged()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock
                {
                    Text = "foo",
                    SelectionStart = 3,
                    SelectionEnd = 3
                };

                target.Text = "a";

                Assert.Equal(1, target.SelectionStart);
                Assert.Equal(1, target.SelectionEnd);
            }
        }

        [Theory]
        [InlineData(typeof(TimeoutException))]
        [InlineData(typeof(OperationCanceledException))]
        [InlineData(typeof(UnauthorizedAccessException))]
        [InlineData(typeof(COMException))]
        public void Copy_Does_Not_Throw_When_Clipboard_Fails(Type exceptionType)
        {
            using var app = UnitTestApplication.Start(ClipboardServices);

            var clipboardImpl = new ThrowingClipboardImplStub(exceptionType);
            var target = CreateSelectableTextBlockInTopLevel(clipboardImpl);
            var messages = new List<string>();

            using (TestLogSink.Start((_, _, _, message, _) => messages.Add(message)))
            {
                using var syncContext = UnitTestSynchronizationContext.Begin();

                target.Copy();

                Assert.Null(Record.Exception(syncContext.ExecutePostedCallbacks));
            }

            Assert.Equal(1, clipboardImpl.SetDataCount);
            Assert.Equal(["Failed to write text to clipboard: {Error}"], messages);
        }

        [Fact]
        public void Copy_Does_Not_Swallow_Unexpected_Exceptions()
        {
            using var app = UnitTestApplication.Start(ClipboardServices);

            var clipboardImpl = new ThrowingClipboardImplStub(typeof(InvalidOperationException));
            var target = CreateSelectableTextBlockInTopLevel(clipboardImpl);

            using var syncContext = UnitTestSynchronizationContext.Begin();

            target.Copy();

            Assert.IsType<InvalidOperationException>(Record.Exception(syncContext.ExecutePostedCallbacks));
        }

        [Theory]
        [InlineData(2, 2, false)]
        [InlineData(1, 3, true)]
        [InlineData(3, 1, true)]
        [InlineData(0, 4, true)]
        public void CanCopy_Tracks_Whether_Selection_Covers_Any_Character(int start, int end, bool expected)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock { Text = "abcd" };

                target.Measure(Size.Infinity);

                target.SelectionStart = start;
                target.SelectionEnd = end;

                Assert.Equal(expected, target.CanCopy);
            }
        }

        [Fact]
        public void CanCopy_Tracks_Selection_Over_Inlines()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new SelectableTextBlock();

                target.Inlines!.Add(new Run("foo"));
                target.Inlines!.Add(new Run("bar"));

                target.Measure(Size.Infinity);

                Assert.False(target.CanCopy);

                target.SelectionStart = 2;
                target.SelectionEnd = 5;

                Assert.True(target.CanCopy);

                target.ClearSelection();

                Assert.False(target.CanCopy);
            }
        }

        private static TestServices ClipboardServices
            => TestServices.MockThreadingInterface.With(
                assetLoader: new StandardAssetLoader(),
                renderInterface: new HeadlessPlatformRenderInterface(),
                textShaperImpl: new HarfBuzzTextShaper(),
                fontManagerImpl: new TestFontManager());

        private static SelectableTextBlock CreateSelectableTextBlockInTopLevel(IClipboardImpl clipboardImpl)
        {
            var target = new SelectableTextBlock
            {
                Text = "abcd",
                SelectionStart = 1,
                SelectionEnd = 3
            };

            var impl = new Mock<ITopLevelImpl>();
            impl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            impl.Setup(x => x.TryGetFeature(typeof(IClipboard))).Returns(new Clipboard(clipboardImpl));
            impl.SetupGet(x => x.RenderScaling).Returns(1);

            var topLevel = new TestTopLevel(impl.Object)
            {
                Template = new FuncControlTemplate<TestTopLevel>((x, scope) =>
                    new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                    }.RegisterInNameScope(scope)),
                Content = target
            };

            topLevel.ApplyTemplate();
            topLevel.LayoutManager.ExecuteInitialLayoutPass();

            Assert.True(target.CanCopy);

            return target;
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl);
    }
}
