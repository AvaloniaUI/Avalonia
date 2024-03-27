using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Generators.Sandbox;

public class RecordList<T> : Canvas where T : new()
{
    [Content]
    public ObservableCollection<object> gridColumns { get; set; }

}
