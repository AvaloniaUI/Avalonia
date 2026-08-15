#nullable enable

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TextBoxAutomationPeerTests : ScopedTestBase
{
    private static TestServices Services => TestServices.MockThreadingInterface.With(
        standardCursorFactory: Mock.Of<ICursorFactory>(),
        renderInterface: new HeadlessPlatformRenderInterface(),
        textShaperImpl: new HarfBuzzTextShaper(),
        fontManagerImpl: new TestFontManager(),
        assetLoader: new StandardAssetLoader());

    private static (TextBox TextBox, TextBoxAutomationPeer Peer) Create(string text, int selectionStart = 0, int selectionEnd = 0)
    {
        var textBox = new TextBox
        {
            Template = TextBoxTests.CreateTemplate(),
            Text = text,
            SelectionStart = selectionStart,
            SelectionEnd = selectionEnd,
        };

        var root = new TestRoot { Child = textBox };
        textBox.ApplyTemplate();
        root.LayoutManager.ExecuteInitialLayoutPass();

        var peer = (TextBoxAutomationPeer)ControlAutomationPeer.CreatePeerForElement(textBox);
        return (textBox, peer);
    }

    [Fact]
    public void GetSelection_Returns_Degenerate_Range_At_Caret_When_Nothing_Selected()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (textBox, peer) = Create("hello world", selectionStart: 4, selectionEnd: 4);

            var selection = Assert.Single(peer.GetSelection());

            Assert.Equal(4, textBox.CaretIndex);
            Assert.Equal("", selection.GetText(-1));
            Assert.True(selection.Compare(selection.Clone()));
        }
    }

    [Fact]
    public void GetSelection_Returns_Normalized_Range_For_Forward_Selection()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello world", selectionStart: 2, selectionEnd: 5);

            var selection = Assert.Single(peer.GetSelection());

            Assert.Equal("llo", selection.GetText(-1));
        }
    }

    [Fact]
    public void GetSelection_Returns_Normalized_Range_For_Reverse_Selection()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello world", selectionStart: 5, selectionEnd: 2);

            var selection = Assert.Single(peer.GetSelection());

            Assert.Equal("llo", selection.GetText(-1));
        }
    }

    [Fact]
    public void DocumentRange_GetText_Returns_Whole_Text()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (textBox, peer) = Create("hello world");

            Assert.Equal(textBox.Text, peer.DocumentRange.GetText(-1));
        }
    }

    [Fact]
    public void Move_By_Character_Shifts_Range_And_Clamps_At_Bounds()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello", selectionStart: 0, selectionEnd: 0);

            var range = peer.GetSelection()[0];

            var moved = range.Move(TextUnit.Character, 3);
            Assert.Equal(3, moved);
            Assert.Equal("", range.GetText(-1));

            // Clamp: only 2 characters left to move before hitting the end of "hello" (index 5).
            moved = range.Move(TextUnit.Character, 10);
            Assert.Equal(2, moved);
        }
    }

    [Fact]
    public void MoveEndpointByUnit_Character_Extends_End_Without_Moving_Start()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello", selectionStart: 1, selectionEnd: 1);

            var range = peer.GetSelection()[0];
            var moved = range.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 3);

            Assert.Equal(3, moved);
            Assert.Equal("ell", range.GetText(-1));
        }
    }

    [Fact]
    public void ExpandToEnclosingUnit_Word_Selects_Whole_Word()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello world", selectionStart: 7, selectionEnd: 7);

            var range = peer.GetSelection()[0];
            range.ExpandToEnclosingUnit(TextUnit.Word);

            Assert.Equal("world", range.GetText(-1));
        }
    }

    [Fact]
    public void GetBoundingRectangles_Returns_NonEmpty_Rects_For_NonDegenerate_Range()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello world", selectionStart: 0, selectionEnd: 5);

            var rects = peer.GetSelection()[0].GetBoundingRectangles();

            Assert.NotEmpty(rects);
        }
    }

    [Fact]
    public void GetBoundingRectangles_Returns_Empty_For_Degenerate_Range()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (_, peer) = Create("hello world", selectionStart: 3, selectionEnd: 3);

            var rects = peer.GetSelection()[0].GetBoundingRectangles();

            Assert.Empty(rects);
        }
    }

    [Fact]
    public void Select_Updates_Caret_And_Selection_Together()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (textBox, peer) = Create("hello world");

            var target = peer.GetSelection()[0];
            target.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 5);
            target.Select();

            Assert.Equal(0, textBox.SelectionStart);
            Assert.Equal(5, textBox.SelectionEnd);
            Assert.Equal(5, textBox.CaretIndex);
        }
    }

    [Fact]
    public void Text_Change_Raises_TextChanged_And_Value_PropertyChanged()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (textBox, peer) = Create("hello");

            var textChangedRaised = 0;
            var valueChangedRaised = 0;
            peer.TextChanged += (_, _) => textChangedRaised++;
            peer.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValuePatternIdentifiers.ValueProperty)
                    valueChangedRaised++;
            };

            textBox.Text = "goodbye";

            Assert.Equal(1, textChangedRaised);
            Assert.Equal(1, valueChangedRaised);
        }
    }

    [Fact]
    public void SelectAll_Raises_Single_Coalesced_TextSelectionChanged()
    {
        using (UnitTestApplication.Start(Services))
        {
            var (textBox, peer) = Create("hello world");

            var raised = 0;
            peer.TextSelectionChanged += (_, _) => raised++;

            textBox.SelectAll();

            // The coalescing raise is posted to the dispatcher; pump the queue so the posted
            // continuation runs, then assert it fired exactly once for the
            // SelectionStart+SelectionEnd(+CaretIndex) burst.
#pragma warning disable xUnit1051 // no async test context/cancellation applies to this synchronous dispatcher pump
            Dispatcher.UIThread.RunJobs();
#pragma warning restore xUnit1051

            Assert.Equal(1, raised);
        }
    }
}
