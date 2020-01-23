using System;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Models
{
    public class ConsoleHistoryItem
    {
        public ConsoleHistoryItem(string input, object output)
        {
            Input = input;
            Output = output;
            Foreground = output is Exception ? Brushes.Red : Brushes.Green;
        }

        public string Input { get; }
        public object Output { get; }
        public IBrush Foreground { get; }
    }
}
