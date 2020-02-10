// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace Avalonia.MessageBox
{
    public class MessageBoxDialog : Window
    {
        public MessageBoxButton Result;
        public string Message { get; private set; }
        public string Caption { get; private set; }
        public MessageBoxButton Buttons { get; private set; }
        public Bitmap MessageIcon { get; private set; }
        public bool HasIcon => MessageIcon != null;
        public bool HasOkButton => (Buttons & MessageBoxButton.OK) != 0;
        public bool HasCancelButton => (Buttons & MessageBoxButton.Cancel) != 0;
        public bool HasYesButton => (Buttons & MessageBoxButton.Yes) != 0;
        public bool HasNoButton => (Buttons & MessageBoxButton.No) != 0;
        public MessageBoxDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public void Setup(string message, string caption, MessageBoxButton buttons, Bitmap icon, Window owner)
        {
            Message = message;
            Caption = caption;
            Buttons = buttons;
            MessageIcon = icon;
            Icon = owner.Icon;
            DataContext = this;
        }
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxButton.OK;
            Close();
        }
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxButton.Cancel;
            Close();
        }
        private void OnYesClick(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxButton.Yes;
            Close();
        }
        private void OnNoClick(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxButton.No;
            Close();
        }
    }
}
