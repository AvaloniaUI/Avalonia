#nullable enable

using Avalonia.Controls;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorTestsBase
{
    protected class CompositorCanvas : CompositorTestServices
    {
        public Canvas Canvas { get; } = new();

        public CompositorCanvas()
        {
            TopLevel.Content = Canvas;
            RunJobs();
            Events.Reset();
        }
    }
}
