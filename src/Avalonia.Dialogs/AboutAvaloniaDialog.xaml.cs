// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls; 
using Avalonia.Markup.Xaml;

namespace Avalonia.Dialogs
{
    public class AboutAvaloniaDialog : Window
    {
        public AboutAvaloniaDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
