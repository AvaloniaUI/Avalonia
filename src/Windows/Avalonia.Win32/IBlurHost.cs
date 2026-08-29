namespace Avalonia.Win32;

internal enum BlurEffect
{
    None,
    GaussianBlur,
    Acrylic,
    MicaLight,
    MicaDark
}

internal interface ICompositionEffectsSurface
{
    bool IsBlurSupported(BlurEffect effect);

    void SetBlur(BlurEffect enable);
}
