using System;
using System.Linq;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Styling.Activators
{
    internal abstract class ContainerQueryActivatorBase : StyleActivatorBase, IStyleActivatorSink
    {
        private readonly StyledElement _target;
        private readonly string? _containerName;
        private Visual? _visual;
        private Layoutable? _currentScreenSizeProvider;

        public ContainerQueryActivatorBase(StyledElement target, string? containerName = null)
        {
            _target = target;
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

        private void Target_DetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
        {
            SetVisual(null);
            ReevaluateIsActive();
        }

        private void Target_AttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
        {
            SetVisual(GetVisual(_target));
            InitializeScreenSizeProvider();
        }

        protected Layoutable? CurrentContainer => _currentScreenSizeProvider;

        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

        protected override void Initialize()
        {
            if (_target is not Visual)
            {
                _target.AttachedToLogicalTree += Target_AttachedToLogicalTree;
                _target.DetachedFromLogicalTree += Target_DetachedFromLogicalTree;
            }

            SetVisual(GetVisual(_target));
            InitializeScreenSizeProvider();
        }

        protected override void Deinitialize()
        {
            if (_target is not Visual)
            {
                _target.AttachedToLogicalTree -= Target_AttachedToLogicalTree;
                _target.DetachedFromLogicalTree -= Target_DetachedFromLogicalTree;
            }

            SetVisual(null);
        }

        private void SetVisual(Visual? visual)
        {
            if (_visual == visual)
            {
                return;
            }

            DeInitializeScreenSizeProvider();

            if (_visual is { } oldVisual)
            {
                oldVisual.AttachedToVisualTree -= Visual_AttachedToVisualTree;
                oldVisual.DetachedFromVisualTree -= Visual_DetachedFromVisualTree;
            }

            _visual = visual;

            if (_visual is { } newVisual)
            {
                newVisual.AttachedToVisualTree += Visual_AttachedToVisualTree;
                newVisual.DetachedFromVisualTree += Visual_DetachedFromVisualTree;
            }
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
            if (_currentScreenSizeProvider == null && GetContainer(_target, _containerName) is { } container && Container.GetQueryProvider(container) is { } provider)
            {
                _currentScreenSizeProvider = container;

                provider.WidthChanged += WidthChanged;
                provider.HeightChanged += HeightChanged;
            }

            ReevaluateIsActive();
        }

        internal static Layoutable? GetContainer(StyledElement target, string? containerName)
        {
            var visual = GetVisual(target);
            if (visual is null)
            {
                return null;
            }

            // The first visual logical ancestor can itself be the container for a non-visual target.
            var ancestors = target is Visual ? visual.GetVisualAncestors() : visual.GetSelfAndVisualAncestors();

            return ancestors.OfType<Layoutable>().FirstOrDefault(layoutable =>
                (containerName == null && Container.GetSizing(layoutable) != ContainerSizing.Normal)
                || (containerName != null && Container.GetName(layoutable) == containerName));
        }

        private static Visual? GetVisual(StyledElement target)
            => target as Visual ?? target.GetLogicalAncestors().OfType<Visual>().FirstOrDefault();

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
