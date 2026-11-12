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

    /// <summary>
    /// The page being shown, when it is this section's own page or one of its pages.
    /// </summary>
    public PageItem? CurrentPage
    {
        get;
        set
        {
            if (RaiseAndSetIfChanged(ref field, value))
            {
                RaisePropertyChanged(nameof(IsCurrent));
                RaisePropertyChanged(nameof(ShowsSelection));
            }
        }
    }

    public bool IsCurrent => CurrentPage is not null;

    /// <summary>
    /// Whether the section's pages are listed. Set when the user opens a section, when a search matches,
    /// and automatically for the section holding the current page.
    /// </summary>
    public bool IsExpanded
    {
        get;
        set
        {
            if (RaiseAndSetIfChanged(ref field, value))
            {
                RaisePropertyChanged(nameof(ShowsSelection));
            }
        }
    }

    /// <summary>
    /// The section carries the selection marker for its own page, and for one of its pages while they are
    /// hidden. Once it is open the current page carries it, so the drawer never shows two markers at once.
    /// </summary>
    public bool ShowsSelection => CurrentPage == PageItem || (IsCurrent && !IsExpanded);

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
