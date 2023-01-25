using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.Themes.Simple
{
    public class SimpleTheme : Styles
    {
        public static readonly StyledProperty<SimpleThemeMode> ModeProperty =
            AvaloniaProperty.Register<SimpleTheme, SimpleThemeMode>(nameof(Mode));

        private readonly IResourceDictionary _simpleDark;
        private readonly IResourceDictionary _simpleLight;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTheme"/> class.
        /// </summary>
        /// <param name="sp">The parent's service provider.</param>
        public SimpleTheme(IServiceProvider? sp = null)
        {
            AvaloniaXamlLoader.Load(sp, this);

            _simpleDark = (IResourceDictionary)GetAndRemove("BaseDark");
            _simpleLight = (IResourceDictionary)GetAndRemove("BaseLight");
            EnsureThemeVariant();

            object GetAndRemove(string key)
            {
                var val = Resources[key]
                    ?? throw new KeyNotFoundException($"Key {key} was not found in the resources");
                Resources.Remove(key);
                return val;
            }
        }

        /// <summary>
        /// Gets or sets the mode of the fluent theme (light, dark).
        /// </summary>
        public SimpleThemeMode Mode
        {
            get => GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
 
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ModeProperty)
            {
                EnsureThemeVariant();
            }
        }

        private void EnsureThemeVariant()
        {
            var themeVariantResource = Mode == SimpleThemeMode.Dark ? _simpleDark : _simpleLight;
            var dict = Resources.MergedDictionaries;
            if (dict.Count == 0)
            {
                dict.Add(themeVariantResource);
            }
            else
            {
                dict[0] = themeVariantResource;
            }
        }
    }
}
