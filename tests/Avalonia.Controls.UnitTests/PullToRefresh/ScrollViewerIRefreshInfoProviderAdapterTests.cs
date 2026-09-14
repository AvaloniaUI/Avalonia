using System.Linq;
using Avalonia.Controls.PullToRefresh;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.PullToRefresh;

public class ScrollViewerIRefreshInfoProviderAdapterTests : ScopedTestBase
{
    // Repro for the "gesture recognizer leaked across Adapt() calls" bug.
    //
    // Adapt() cleans up the previous ScrollViewer's pointer handlers and the previous
    // RefreshInfoProvider's pull-event handlers, but never removes the previously-created
    // ScrollablePullGestureRecognizer from _interactionSource.GestureRecognizers.
    // Each subsequent Adapt() instantiates and adds a new recognizer, so the input element
    // ends up holding N recognizers after N calls. They all listen for the same pointer
    // events and raise duplicate PullGesture/PullGestureEnded pairs, which (combined with
    // the _entered desync fix in RefreshInfoProvider) corrupts the visualizer state.
    [Fact]
    public void Adapt_Called_Twice_Does_Not_Leak_Pull_Gesture_Recognizer()
    {
        var sv = new ScrollViewer
        {
            Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
            Content = new Border(),
        };

        // Wrap in a TestRoot and execute the layout pass so Loaded fires and the
        // visual tree under the ScrollContentPresenter is fully wired up
        // (otherwise the adapter never reaches MakeInteractionSource).
        var root = new TestRoot(sv);
        root.LayoutManager.ExecuteInitialLayoutPass();

        var adapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection.TopToBottom, isMouseEnabled: false);

        adapter.Adapt(sv, new Size(100, 100));
        var interactionSource = adapter.InteractionSource;
        Assert.NotNull(interactionSource);
        var afterFirst = interactionSource.GestureRecognizers.OfType<ScrollablePullGestureRecognizer>().Count();
        Assert.Equal(1, afterFirst);

        adapter.Adapt(sv, new Size(100, 100));
        var interactionSourceAfter = adapter.InteractionSource;
        Assert.NotNull(interactionSourceAfter);
        var afterSecond = interactionSourceAfter.GestureRecognizers.OfType<ScrollablePullGestureRecognizer>().Count();
        Assert.Equal(1, afterSecond);
    }
}
