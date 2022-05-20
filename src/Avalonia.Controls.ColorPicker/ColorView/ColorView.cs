using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presents a color for user editing using a spectrum, palette and component sliders.
    /// </summary>
    public partial class ColorView : TemplatedControl
    {
        private ObservableCollection<Color> _customPaletteColors = new ObservableCollection<Color>();


    }
}
