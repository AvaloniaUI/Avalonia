using System.Collections.Generic;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Models;

public class HomeSection(string title, IReadOnlyList<PageItem> items)
{
    public string Title { get; } = title;
    public IReadOnlyList<PageItem> Items { get; } = items;
}
