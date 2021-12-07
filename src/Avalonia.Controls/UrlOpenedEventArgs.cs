using System;

namespace Avalonia
{
    public class UrlOpenedEventArgs : EventArgs
    {
        public UrlOpenedEventArgs(string[] urls)
        {
            Urls = urls;
        }
        
        public string[] Urls { get; }
    }
}
