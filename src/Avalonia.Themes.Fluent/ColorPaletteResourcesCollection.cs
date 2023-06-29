using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent;

internal class ColorPaletteResourcesCollection : AvaloniaDictionary<ThemeVariant, ColorPaletteResources>, IResourceProvider
{
    public ColorPaletteResourcesCollection() : base(2)
    {
        this.ForEachItem(
            (key, x) =>
            {
                if (Owner is not null)
                {
                    x.PropertyChanged += Palette_PropertyChanged;
                }

                if (key != ThemeVariant.Dark && key != ThemeVariant.Light)
                {
                    throw new InvalidOperationException(
                        $"{nameof(FluentTheme)}.{nameof(FluentTheme.Palettes)} only supports Light and Dark variants.");
                }
            },
            (_, x) =>
            {
                if (Owner is not null)
                {
                    x.PropertyChanged -= Palette_PropertyChanged;
                }
            },
            () => throw new NotSupportedException("Dictionary reset not supported"));
    }

    public bool HasResources => Count > 0;
    public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (theme == null || theme == ThemeVariant.Default)
        {
            theme = ThemeVariant.Light;
        }

        if (base.TryGetValue(theme, out var themePaletteResources)
            && themePaletteResources.TryGetResource(key, theme, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    public IResourceHost? Owner { get; private set; }
    public event EventHandler? OwnerChanged;
    public void AddOwner(IResourceHost owner)
    {
        Owner = owner;
        OwnerChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveOwner(IResourceHost owner)
    {
        Owner = null;
        OwnerChanged?.Invoke(this, EventArgs.Empty);
    }
            
    private void Palette_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ColorPaletteResources.AccentProperty)
        {
            Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
        }
    }
}
