using System;
using Avalonia.Reactive;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    public static class ResourceNodeExtensions
    {
        /// <summary>
        /// Finds the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource, or <see cref="AvaloniaProperty.UnsetValue"/> if not found.</returns>
        public static object? FindResource(this IResourceHost control, object key)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            if (control.TryFindResource(key, out var value))
            {
                return value;
            }

            return AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Tries to the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="value">On return, contains the resource if found, otherwise null.</param>
        /// <returns>True if the resource was found; otherwise false.</returns>
        public static bool TryFindResource(this IResourceHost control, object key, out object? value)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return control.TryFindResource(key, null, out value);
        }

        /// <summary>
        /// Finds the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="theme">Theme used to select theme dictionary.</param>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource, or <see cref="AvaloniaProperty.UnsetValue"/> if not found.</returns>
        public static object? FindResource(this IResourceHost control, ThemeVariant? theme, object key)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            if (control.TryFindResource(key, theme, out var value))
            {
                return value;
            }

            return AvaloniaProperty.UnsetValue;
        }
        
        /// <summary>
        /// Tries to the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="theme">Theme used to select theme dictionary.</param>
        /// <param name="value">On return, contains the resource if found, otherwise null.</param>
        /// <returns>True if the resource was found; otherwise false.</returns>
        public static bool TryFindResource(this IResourceHost control, object key, ThemeVariant? theme, out object? value)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            IResourceHost? current = control;

            while (current != null)
            {
                if (current.TryGetResource(key, theme, out value))
                {
                    return true;
                }

                current = (current as IStyleHost)?.StylingParent as IResourceHost;
            }

            value = null;
            return false;
        }
        
        /// <inheritdoc cref="IResourceNode.TryGetResource" />
        public static bool TryGetResource(this IResourceHost control, object key, out object? value)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return control.TryGetResource(key, null, out value);
        }

        public static IObservable<object?> GetResourceObservable(
            this IResourceHost control,
            object key,
            Func<object?, object?>? converter = null)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new ResourceObservable(control, key, converter);
        }

        public static IObservable<object?> GetResourceObservable(
            this IResourceProvider resourceProvider,
            object key,
            Func<object?, object?>? converter = null)
        {
            resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new FloatingResourceObservable(resourceProvider, key, null, converter);
        }

        public static IObservable<object?> GetResourceObservable(
            this IResourceProvider resourceProvider,
            object key,
            ThemeVariant? defaultThemeVariant,
            Func<object?, object?>? converter = null)
        {
            resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new FloatingResourceObservable(resourceProvider, key, defaultThemeVariant, converter);
        }

        private class ResourceObservable : LightweightObservableBase<object?>
        {
            private readonly IResourceHost _target;
            private readonly object _key;
            private readonly Func<object?, object?>? _converter;

            public ResourceObservable(
                IResourceHost target,
                object key,
                Func<object?, object?>? converter)
            {
                _target = target;
                _key = key;
                _converter = converter;
            }

            protected override void Initialize()
            {
                _target.ResourcesChanged += ResourcesChanged;
                if (_target is IThemeVariantHost themeVariantHost)
                {
                    themeVariantHost.ActualThemeVariantChanged += ActualThemeVariantChanged;
                }
            }

            protected override void Deinitialize()
            {
                _target.ResourcesChanged -= ResourcesChanged;
                if (_target is IThemeVariantHost themeVariantHost)
                {
                    themeVariantHost.ActualThemeVariantChanged -= ActualThemeVariantChanged;
                }
            }

            protected override void Subscribed(IObserver<object?> observer, bool first)
            {
                observer.OnNext(GetValue());
            }

            private void ResourcesChanged(object? sender, ResourcesChangedEventArgs e)
            {
                PublishNext(GetValue());
            }

            private void ActualThemeVariantChanged(object? sender, EventArgs e)
            {
                PublishNext(GetValue());
            }

            private object? GetValue()
            {
                var theme = (_target as IThemeVariantHost)?.ActualThemeVariant; 
                var value = _target.FindResource(theme, _key) ?? AvaloniaProperty.UnsetValue;

                return _converter?.Invoke(value) ?? value;
            }
        }

        private class FloatingResourceObservable : LightweightObservableBase<object?>
        {
            private readonly IResourceProvider _target;
            private readonly ThemeVariant? _overrideThemeVariant;
            private readonly object _key;
            private readonly Func<object?, object?>? _converter;
            private IResourceHost? _owner;

            public FloatingResourceObservable(
                IResourceProvider target,
                object key,
                ThemeVariant? overrideThemeVariant,
                Func<object?, object?>? converter)
            {
                _target = target;
                _key = key;
                _overrideThemeVariant = overrideThemeVariant;
                _converter = converter;
            }

            protected override void Initialize()
            {
                _target.OwnerChanged += OwnerChanged;
                _owner = _target.Owner;

                if (_owner is not null)
                {
                    _owner.ResourcesChanged += ResourcesChanged;
                }
                if (_overrideThemeVariant is null && _owner is IThemeVariantHost themeVariantHost)
                {
                    themeVariantHost.ActualThemeVariantChanged += ActualThemeVariantChanged;
                }
            }

            protected override void Deinitialize()
            {
                _target.OwnerChanged -= OwnerChanged;

                if (_owner is not null)
                {
                    _owner.ResourcesChanged -= ResourcesChanged;
                }
                if (_overrideThemeVariant is null && _owner is IThemeVariantHost themeVariantHost)
                {
                    themeVariantHost.ActualThemeVariantChanged -= ActualThemeVariantChanged;
                }

                _owner = null;
            }

            protected override void Subscribed(IObserver<object?> observer, bool first)
            {
                if (_target.Owner is not null)
                {
                    observer.OnNext(GetValue());
                }
            }

            private void PublishNext()
            {
                if (_target.Owner is not null)
                {
                    PublishNext(GetValue());
                }
            }

            private void OwnerChanged(object? sender, EventArgs e)
            {
                if (_owner is not null)
                {
                    _owner.ResourcesChanged -= ResourcesChanged;
                }
                if (_overrideThemeVariant is null && _owner is IThemeVariantHost themeVariantHost)
                {
                    themeVariantHost.ActualThemeVariantChanged -= ActualThemeVariantChanged;
                }

                _owner = _target.Owner;

                if (_owner is not null)
                {
                    _owner.ResourcesChanged += ResourcesChanged;
                }
                if (_overrideThemeVariant is null && _owner is IThemeVariantHost themeVariantHost2)
                {
                    themeVariantHost2.ActualThemeVariantChanged += ActualThemeVariantChanged;
                }

                PublishNext();
            }

            private void ActualThemeVariantChanged(object? sender, EventArgs e)
            {
                PublishNext();
            }

            private void ResourcesChanged(object? sender, ResourcesChangedEventArgs e)
            {
                PublishNext();
            }

            private object? GetValue()
            {
                var theme = _overrideThemeVariant ?? (_target.Owner as IThemeVariantHost)?.ActualThemeVariant; 
                var value = _target.Owner?.FindResource(theme, _key) ?? AvaloniaProperty.UnsetValue;

                return _converter?.Invoke(value) ?? value;
            }
        }
    }
}
