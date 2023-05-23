namespace Avalonia.Win32.Interoperability.Wpf;

internal record struct IntSize(double Width, double Height)
{
    public static implicit  operator IntSize(System.Windows.Size size)
    {
        return new IntSize {Width = (int) size.Width, Height = (int) size.Height};
    }
}
