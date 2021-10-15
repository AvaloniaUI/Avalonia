namespace Avalonia.Win32.WinRT
{
    public enum BlurEffect
    {
        None,
        Acrylic,
        Mica
    }
    
    internal interface IBlurHost
    {
        void SetBlur(BlurEffect enable);
    }
}
