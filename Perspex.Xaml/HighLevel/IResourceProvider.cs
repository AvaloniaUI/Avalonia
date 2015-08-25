namespace Perspex.Xaml.HighLevel
{
    using System;
    using System.IO;

    public interface IResourceProvider
    {
        Stream GetStream(Uri uri);
    }
}