namespace Avalonia.Win32.WinRT
{
    public enum BlurEffect
    {
        None,
        Acrylic,
        Mica
    }
    
    public interface IBlurHost
    {
        void SetBlur(BlurEffect enable);
    }
}
