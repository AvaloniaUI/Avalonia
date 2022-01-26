using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace Sandbox;

using ReactiveUI;

public partial class LabelViewModel : ReactiveObject
{
    public bool IsBlackListed { get; set; }
    
    private bool _isPointerOver;
    private string _value;
    

    public LabelViewModel(PrivacyControlViewModel owner, string label)
    {
        Pockets = new List<PocketViewModel>();
        _value = "Normal";

        this.WhenAnyValue(x => x.IsPointerOver)
            .Subscribe(x => Value = x ? "PointerOver" : "Normal");

        ClickedCommand = ReactiveCommand.Create(() =>
        {
            owner.SwapLabel(this);
        });
    }

    public bool IsPointerOver
    {
        get => _isPointerOver;

        set => this.RaiseAndSetIfChanged(ref _isPointerOver, value);
    }

    public List<PocketViewModel> Pockets { get; }

    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public ICommand ClickedCommand { get; }
}

public class PocketViewModel : ReactiveObject
{
    public PocketViewModel()
    {
        Labels = new List<LabelViewModel>();
    }

    public List<LabelViewModel> Labels { get; }
}

public class PrivacyControlViewModel
{
    public PrivacyControlViewModel()
    {
        WhiteList(new LabelViewModel(this, "Dan"));
        WhiteList(new LabelViewModel(this, "Steven"));
        WhiteList(new LabelViewModel(this, "Nikita"));
        WhiteList(new LabelViewModel(this, "Jumar"));
        WhiteList(new LabelViewModel(this, "Tako"));
    }
    
    public ObservableCollection<LabelViewModel> LabelsWhiteList { get; } = new();

    public ObservableCollection<LabelViewModel> LabelsBlackList { get; } = new();
    
    internal void SwapLabel(LabelViewModel label)
    {
        if (label.IsBlackListed)
        {
            WhiteList(label);
        }
        else
        {
            BlackList(label);
        }
    }

    private void WhiteList(LabelViewModel label)
    {
        LabelsBlackList.Remove(label);

        label.IsBlackListed = false;
        label.IsPointerOver = false;

        LabelsWhiteList.Add(label);
    }

    private void BlackList(LabelViewModel label)
    {
        LabelsWhiteList.Remove(label);

        label.IsBlackListed = true;
        label.IsPointerOver = false;

        LabelsBlackList.Add(label);
    }
}

public class BindPointerOverBehavior : Behavior<Control>
{
    public static readonly StyledProperty<bool> IsPointerOverProperty =
        AvaloniaProperty.Register<BindPointerOverBehavior, bool>(nameof(IsPointerOver), defaultBindingMode: BindingMode.OneWayToSource);

    public bool IsPointerOver
    {
        get => GetValue(IsPointerOverProperty);
        set => SetValue(IsPointerOverProperty, value);
    }

    protected override void OnAttached()
    {
        AssociatedObject.PropertyChanged += AssociatedObjectOnPropertyChanged;
    }

    private void AssociatedObjectOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsPointerOverProperty)
        {
            IsPointerOver = e.NewValue is true;
        }
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PropertyChanged -= AssociatedObjectOnPropertyChanged;

        base.OnDetaching();

        IsPointerOver = false;
    }
}
