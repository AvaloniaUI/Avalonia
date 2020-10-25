namespace Avalonia.Win32
{
    internal class CompositionBlurHost : IBlurHost
    {
        Windows.UI.Composition.Visual _blurVisual;

        public CompositionBlurHost(Windows.UI.Composition.Visual blurVisual)
        {
            _blurVisual = blurVisual;
        }

        public void SetBlur(bool enabled)
        {
            _blurVisual.IsVisible = enabled;
        }
    }
}

