using Avalonia.Collections;
using Avalonia.Media;
using ControlCatalog.Models;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    internal class CompositionAnimationViewModel : ViewModelBase
    {
        public AvaloniaList<CompositionPageColorItem> ColorItems { get; }

        public CompositionAnimationViewModel()
        {
            ColorItems = CreateColorItems();
        }

        private static AvaloniaList<CompositionPageColorItem> CreateColorItems()
        {
            return new AvaloniaList<CompositionPageColorItem>
            {
                new CompositionPageColorItem(Color.FromArgb(255, 255, 185, 0)),
                new CompositionPageColorItem(Color.FromArgb(255, 231, 72, 86)),
                new CompositionPageColorItem(Color.FromArgb(255, 0, 120, 215)),
                new CompositionPageColorItem(Color.FromArgb(255, 0, 153, 188)),
                new CompositionPageColorItem(Color.FromArgb(255, 122, 117, 116)),
                new CompositionPageColorItem(Color.FromArgb(255, 118, 118, 118)),
                new CompositionPageColorItem(Color.FromArgb(255, 255, 141, 0)),
                new CompositionPageColorItem(Color.FromArgb(255, 232, 17, 35)),
                new CompositionPageColorItem(Color.FromArgb(255, 0, 99, 177)),
                new CompositionPageColorItem(Color.FromArgb(255, 45, 125, 154)),
                new CompositionPageColorItem(Color.FromArgb(255, 93, 90, 88)),
                new CompositionPageColorItem(Color.FromArgb(255, 76, 74, 72)),
                new CompositionPageColorItem(Color.FromArgb(255, 247, 99, 12)),
                new CompositionPageColorItem(Color.FromArgb(255, 234, 0, 94)),
                new CompositionPageColorItem(Color.FromArgb(255, 142, 140, 216)),
                new CompositionPageColorItem(Color.FromArgb(255, 0, 183, 195)),
                new CompositionPageColorItem(Color.FromArgb(255, 104, 118, 138)),
                new CompositionPageColorItem(Color.FromArgb(255, 105, 121, 126)),
                new CompositionPageColorItem(Color.FromArgb(255, 202, 80, 16)),
                new CompositionPageColorItem(Color.FromArgb(255, 195, 0, 82)),
                new CompositionPageColorItem(Color.FromArgb(255, 107, 105, 214)),
                new CompositionPageColorItem(Color.FromArgb(255, 3, 131, 135)),
                new CompositionPageColorItem(Color.FromArgb(255, 81, 92, 107)),
                new CompositionPageColorItem(Color.FromArgb(255, 74, 84, 89)),
                new CompositionPageColorItem(Color.FromArgb(255, 218, 59, 1))
            };
        }
    }
}
