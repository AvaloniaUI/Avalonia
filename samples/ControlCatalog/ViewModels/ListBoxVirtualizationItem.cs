using System.Linq;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ListBoxVirtualizationItem : ViewModelBase
    {
        private static readonly RotateTransform HorizontalTextTransform = new(-90);
        private double _size;
        private double _fontSize = 14;
        private bool _isVisible = true;
        private bool _horizontal;
        private bool _isHeader;

        public ListBoxVirtualizationItem(int id, double size, bool wraps)
        {
            Id = id;
            _size = size;
            Wraps = wraps;
            Text = string.Concat(Enumerable.Repeat(
                "Resize the cross axis to wrap this text. The visible item should stay anchored. ", 1 + id % 3));
        }

        public int Id { get; }
        public bool Wraps { get; }
        public string Text { get; }
        public bool ShowText => Wraps && !IsHeader;
        public string Caption => IsHeader ? $"Fixed header {Id}" : $"Item {Id} — {(Wraps ? $"font {FontSize:0.#}" : $"{Size:0.#} px")}";
        public double Width => _horizontal && (!Wraps || IsHeader) ? Size : double.NaN;
        public double Height => !_horizontal && (!Wraps || IsHeader) ? Size : double.NaN;
        public ITransform? TextTransform => _horizontal && Wraps && !IsHeader ? HorizontalTextTransform : null;

        public double Size
        {
            get => _size;
            set
            {
                if (RaiseAndSetIfChanged(ref _size, value))
                    NotifyLayout();
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (RaiseAndSetIfChanged(ref _fontSize, value))
                    RaisePropertyChanged(nameof(Caption));
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public bool IsHeader
        {
            get => _isHeader;
            set
            {
                if (RaiseAndSetIfChanged(ref _isHeader, value))
                    NotifyLayout();
            }
        }

        public bool Horizontal
        {
            get => _horizontal;
            set
            {
                if (RaiseAndSetIfChanged(ref _horizontal, value))
                    NotifyLayout();
            }
        }

        private void NotifyLayout()
        {
            RaisePropertyChanged(nameof(Width));
            RaisePropertyChanged(nameof(Height));
            RaisePropertyChanged(nameof(TextTransform));
            RaisePropertyChanged(nameof(ShowText));
            RaisePropertyChanged(nameof(Caption));
        }
    }
}
