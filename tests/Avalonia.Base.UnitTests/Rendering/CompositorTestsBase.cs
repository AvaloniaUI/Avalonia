using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorTestsBase : ScopedTestBase
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
