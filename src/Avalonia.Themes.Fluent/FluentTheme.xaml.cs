using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent
{
    public enum FluentThemeMode
    {
        Light,
        Dark,
    }

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
        private readonly IResourceDictionary _baseDark;
        private readonly IResourceDictionary _fluentDark;
        private readonly IResourceDictionary _baseLight;
        private readonly IResourceDictionary _fluentLight;
        private readonly Styles _compactStyles;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentTheme"/> class.
        /// </summary>
        public FluentTheme()
        {
            AvaloniaXamlLoader.Load(this);
            
            _baseDark = (IResourceDictionary)GetAndRemove("BaseDark");
            _fluentDark = (IResourceDictionary)GetAndRemove("FluentDark");
            _baseLight = (IResourceDictionary)GetAndRemove("BaseLight");
            _fluentLight = (IResourceDictionary)GetAndRemove("FluentLight");
            _compactStyles = (Styles)GetAndRemove("CompactStyles");
            
            EnsureThemeVariants();
            EnsureCompactStyles();

            object GetAndRemove(string key)
            {
                var val = Resources[key]
                          ?? throw new KeyNotFoundException($"Key {key} was not found in the resources");
                Resources.Remove(key);
                return val;
            }
        }

        public static readonly StyledProperty<FluentThemeMode> ModeProperty =
            AvaloniaProperty.Register<FluentTheme, FluentThemeMode>(nameof(Mode));

        public static readonly StyledProperty<DensityStyle> DensityStyleProperty =
            AvaloniaProperty.Register<FluentTheme, DensityStyle>(nameof(DensityStyle));

        /// <summary>
        /// Gets or sets the mode of the fluent theme (light, dark).
        /// </summary>
        public FluentThemeMode Mode
        {
            get => GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

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
            
            if (change.Property == ModeProperty)
            {
                EnsureThemeVariants();
            }

            if (change.Property == DensityStyleProperty)
            {
                EnsureCompactStyles();
            }
        }

        private void EnsureThemeVariants()
        {
            var themeVariantResource1 = Mode == FluentThemeMode.Dark ? _baseDark : _baseLight;
            var themeVariantResource2 = Mode == FluentThemeMode.Dark ? _fluentDark : _fluentLight;
            var dict = Resources.MergedDictionaries;
            if (dict.Count == 2)
            {
                dict.Insert(1, themeVariantResource1);
                dict.Add(themeVariantResource2);
            }
            else
            {
                dict[1] = themeVariantResource1;
                dict[3] = themeVariantResource2;
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
