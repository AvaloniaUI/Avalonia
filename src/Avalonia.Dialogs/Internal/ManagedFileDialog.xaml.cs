using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Avalonia.Dialogs.Internal
{
	class ManagedFileDialog : Window
	{
		public ManagedFileDialog()
		{
			AvaloniaXamlLoader.Load(this);
#if DEBUG
			this.AttachDevTools();
#endif
		}
	}
}
