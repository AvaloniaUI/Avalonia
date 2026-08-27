using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.Models;

public class HomeSection(string title, StreamGeometry iconData) : ViewModelBase
{
    public string Title { get; } = title;
    public StreamGeometry IconData { get; } = iconData;
    public IReadOnlyList<PageItem>? Items { get; set; }

    public bool IsSectionVisible => Items?.Any(x => x.IsVisible) == true;

    public void RaiseSectionVisibilityChanged()
    {
        RaisePropertyChanged(nameof(IsSectionVisible));
    }
}
