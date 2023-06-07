using System;
using Avalonia.Platform;

namespace Avalonia.Styling.Activators
{
    internal abstract class MediaQueryActivatorBase : StyleActivatorBase, IStyleActivatorSink
    {
        private readonly Visual _visual;
        private IMediaProvider? _currentScreenSizeProvider;

        public MediaQueryActivatorBase(
            Visual visual)
        {
            _visual = visual;

            _visual.AttachedToVisualTree += Visual_AttachedToVisualTree;
        }

        private void Visual_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            InitializeScreenSizeProvider();
        }

        protected IMediaProvider? CurrentMediaInfoProvider => _currentScreenSizeProvider;

        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

        protected override void Initialize()
        {
            InitializeScreenSizeProvider();
        }

        protected override void Deinitialize()
        {
            _visual.AttachedToVisualTree -= Visual_AttachedToVisualTree;

            if (_currentScreenSizeProvider is { })
            {
                _currentScreenSizeProvider.ScreenSizeChanged -= ScreenSizeChanged;
                _currentScreenSizeProvider = null;
            }
        }

        private void InitializeScreenSizeProvider()
        {
            if (_visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                _currentScreenSizeProvider = mediaProvider;

                _currentScreenSizeProvider.ScreenSizeChanged += ScreenSizeChanged;
            }

            ReevaluateIsActive();
        }

        private void ScreenSizeChanged(object? sender, EventArgs e)
        {
            ReevaluateIsActive();
        }
    }
}
