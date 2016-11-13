using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ControlCatalog
{
	public class MetroWindowTheme : Styles
	{
		public MetroWindowTheme()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}