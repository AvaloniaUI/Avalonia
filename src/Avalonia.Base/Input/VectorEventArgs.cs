using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class VectorEventArgs : RoutedEventArgs
    {
        internal VectorEventArgs()
        {

        }

        public Vector Vector { get; set; }
    }
}
