using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using ControlCatalog.Pages;
using MiniMvvm;

namespace ControlCatalog.Models;

public class HomeSection : ViewModelBase
{
    public string Title { get; }
    public StreamGeometry IconData { get; }
    public IReadOnlyList<PageItem>? Items { get; set; }

    public bool IsSectionVisible => Items?.Any(x => x.IsVisible) == true;

    public PageItem PageItem { get; }

    public HomeSection(string title, StreamGeometry iconData)
    {
        Title = title;
        IconData = iconData;
        PageItem = new PageItem(title, () => new SectionPage(this), iconData, "", null);
    }

    public void RaiseSectionVisibilityChanged()
    {
        RaisePropertyChanged(nameof(IsSectionVisible));
    }
}
