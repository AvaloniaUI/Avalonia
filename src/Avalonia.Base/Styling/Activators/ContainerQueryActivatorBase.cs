using System;
using System.Linq;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Styling.Activators
{
    internal abstract class ContainerQueryActivatorBase : StyleActivatorBase, IStyleActivatorSink
    {
        private readonly Visual _visual;
        private readonly string? _containerName;
        private Layoutable? _currentScreenSizeProvider;

        public ContainerQueryActivatorBase(
            Visual visual, string? containerName = null)
        {
            _visual = visual;
            _containerName = containerName;
        }

        private void Visual_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            DeInitializeScreenSizeProvider();
        }

        private void Visual_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            InitializeScreenSizeProvider();
        }

        protected Layoutable? CurrentContainer => _currentScreenSizeProvider;

        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

        protected override void Initialize()
        {
            InitializeScreenSizeProvider();

            _visual.AttachedToVisualTree += Visual_AttachedToVisualTree;
            _visual.DetachedFromVisualTree += Visual_DetachedFromVisualTree;
        }

        protected override void Deinitialize()
        {
            _visual.AttachedToVisualTree -= Visual_AttachedToVisualTree;
            _visual.DetachedFromVisualTree -= Visual_DetachedFromVisualTree;

            DeInitializeScreenSizeProvider();
        }

        private void DeInitializeScreenSizeProvider()
        {
            if (_currentScreenSizeProvider is { } && Container.GetQueryProvider(_currentScreenSizeProvider) is { } provider)
            {
                provider.WidthChanged -= WidthChanged;
                provider.HeightChanged -= HeightChanged;
                _currentScreenSizeProvider = null;
            }
        }

        private void InitializeScreenSizeProvider()
        {
            if (_currentScreenSizeProvider == null && GetContainer(_visual, _containerName) is { } container && Container.GetQueryProvider(container) is { } provider)
            {
                _currentScreenSizeProvider = container;

                provider.WidthChanged += WidthChanged;
                provider.HeightChanged += HeightChanged;
            }

            ReevaluateIsActive();
        }

        internal static Layoutable? GetContainer(Visual visual, string? containerName)
        {
            return visual.GetVisualAncestors().Where(x => x is Layoutable layoutable &&
                ((containerName == null && Container.GetSizing(layoutable) != ContainerSizing.Normal)
                || (containerName != null && Container.GetName(layoutable) == containerName))).FirstOrDefault() as Layoutable;
        }

        private void HeightChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }

        private void WidthChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }

        private void OrientationChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }
    }
}
