using System;
using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.ViewModels;

public class TabStripPageViewModel : ViewModelBase
{
    private bool _multiple;
    private bool _toggle;
    private bool _alwaysSelected;
    private IObservable<SelectionMode> _selectionMode;

    public TabControlPageViewModelItem[]? Tabs { get; set; }
    public IObservable<SelectionMode> SelectionMode => _selectionMode;

    public TabStripPageViewModel()
    {
        _selectionMode = this.WhenAnyValue(
            x => x.Multiple,
            x => x.Toggle,
            x => x.AlwaysSelected,
            (m, t, a) =>
                (m ? Avalonia.Controls.SelectionMode.Multiple : 0) |
                (t ? Avalonia.Controls.SelectionMode.Toggle : 0) |
                (a ? Avalonia.Controls.SelectionMode.AlwaysSelected : 0));
    }

    public bool Multiple
    {
        get => _multiple;
        set => this.RaiseAndSetIfChanged(ref _multiple, value);
    }

    public bool Toggle
    {
        get => _toggle;
        set => this.RaiseAndSetIfChanged(ref _toggle, value);
    }

    public bool AlwaysSelected
    {
        get => _alwaysSelected;
        set => this.RaiseAndSetIfChanged(ref _alwaysSelected, value);
    }
}
