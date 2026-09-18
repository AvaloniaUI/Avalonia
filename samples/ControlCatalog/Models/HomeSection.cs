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
    /// True when the page being shown belongs to this section.
    /// </summary>
    public bool IsCurrent
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
    /// The section carries the selection marker only while its pages are hidden. Once it is open the
    /// current page carries it, so the drawer never shows two markers at once.
    /// </summary>
    public bool ShowsSelection => IsCurrent && !IsExpanded;

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
