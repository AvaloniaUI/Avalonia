using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent
{
    public enum DensityStyle
    {
        Normal,
        Compact
    }

    /// <summary>
    /// Includes the fluent theme in an application.
    /// </summary>
    public class FluentTheme : Styles
    {
        private readonly Styles _compactStyles;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentTheme"/> class.
        /// </summary>
        /// <param name="sp">The parent's service provider.</param>
        public FluentTheme(IServiceProvider? sp = null)
        {
            AvaloniaXamlLoader.Load(sp, this);
            
            _compactStyles = (Styles)GetAndRemove("CompactStyles");

            EnsureCompactStyles();

            object GetAndRemove(string key)
            {
                var val = Resources[key]
                          ?? throw new KeyNotFoundException($"Key {key} was not found in the resources");
                Resources.Remove(key);
                return val;
            }
        }
        
        public static readonly StyledProperty<DensityStyle> DensityStyleProperty =
            AvaloniaProperty.Register<FluentTheme, DensityStyle>(nameof(DensityStyle));

        /// <summary>
        /// Gets or sets the density style of the fluent theme (normal, compact).
        /// </summary>
        public DensityStyle DensityStyle
        {
            get => GetValue(DensityStyleProperty);
            set => SetValue(DensityStyleProperty, value);
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DensityStyleProperty)
            {
                EnsureCompactStyles();
            }
        }

        private void EnsureCompactStyles()
        {
            if (DensityStyle == DensityStyle.Compact)
            {
                Add(_compactStyles);
            }
            else
            {
                Remove(_compactStyles);
            }
        }
    }
}
