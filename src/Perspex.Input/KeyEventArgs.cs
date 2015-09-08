





namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public class KeyEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice Device { get; set; }

        public Key Key { get; set; }

        public ModifierKeys Modifiers { get; set; }
    }
}
