using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Mixins;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

public class MediaStyles : MediaStylesBase
{
    private IVisual? _currentVisual;
    private CompositeDisposable? _subscriptions;

    public MediaStyles()
    {
        OwnerChanged += (_,_) => Attach();
    }

    private void Initialise()
    {
        if (_currentVisual is not null)
        {
            _currentVisual.AttachedToVisualTree += VisualOnAttachedToVisualTree;
            _currentVisual.DetachedFromVisualTree += VisualOnDetachedFromVisualTree;
        }
    }

    private void Deinitialise()
    {
        if (_currentVisual is not null)
        {
            _currentVisual.AttachedToVisualTree -= VisualOnAttachedToVisualTree;
            _currentVisual.DetachedFromVisualTree -= VisualOnDetachedFromVisualTree;
        }
    }

    private void Attach()
    {
        if (_currentVisual is not null)
        {
            Deinitialise();
            _currentVisual = null;
        }
            
        if (Owner is IVisual visual)
        {
            _currentVisual = visual;
                
            Initialise();
        }
    }

    private void VisualOnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _subscriptions?.Dispose();
    }

    private void VisualOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_currentVisual?.VisualRoot is TopLevel topLevel)
        {
            _subscriptions?.Dispose();

            _subscriptions = new CompositeDisposable();
                
            topLevel.GetObservable(Visual.BoundsProperty)
                .Select(x => x.Size)
                .Subscribe(OnSizeChanged)
                .DisposeWith(_subscriptions);
        }
    }

    private void OnSizeChanged(Size size)
    {
        if (size.Width > 600 && size.Height > 600)
        {
            SetIsActive(true);
        }
        else
        {
            SetIsActive(false);
        }
    }
}

public abstract class MediaStylesBase : AvaloniaObject,
        IAvaloniaList<IStyle>,
        IStyle,
        IResourceProvider
    {
        private readonly AvaloniaList<IStyle> _styles = new();
        private IResourceHost? _owner;
        private IResourceDictionary? _resources;
        private Dictionary<Type, List<IStyle>?>? _cache;
        private readonly List<IStyle> _inactive = new();
        private bool _isActive;

        public MediaStylesBase()
        {
            _styles.ResetBehavior = ResetBehavior.Remove;
            _styles.CollectionChanged += OnCollectionChanged;
        }

        public MediaStylesBase(IResourceHost owner)
            : this()
        {
            Owner = owner;
        }
        
        protected void SetIsActive(bool isActive)
        {
            if (isActive != _isActive)
            {
                _isActive = isActive;

                if (_isActive)
                {
                    foreach (var style in _inactive)
                    {
                        Add(style);
                    }
                    
                    _inactive.Clear();
                }
                else
                {
                    foreach (var style in this)
                    {
                        _inactive.Add(style);
                    }
                    
                    Clear();
                }
            }
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event EventHandler? OwnerChanged;

        public int Count => _styles.Count;

        public IResourceHost? Owner
        {
            get => _owner;
            private set
            {
                if (_owner != value)
                {
                    _owner = value;
                    OwnerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ?? (Resources = new ResourceDictionary());
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(Resources));

                if (Owner is object)
                {
                    _resources?.RemoveOwner(Owner);
                }

                _resources = value;

                if (Owner is object)
                {
                    _resources.AddOwner(Owner);
                }
            }
        }

        bool ICollection<IStyle>.IsReadOnly => false;

        bool IResourceNode.HasResources
        {
            get
            {
                if (_resources?.Count > 0)
                {
                    return true;
                }

                foreach (var i in this)
                {
                    if (i is IResourceProvider p && p.HasResources)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        IStyle IReadOnlyList<IStyle>.this[int index] => _styles[index];

        IReadOnlyList<IStyle> IStyle.Children => this;

        public IStyle this[int index]
        {
            get => _styles[index];
            set => _styles[index] = value;
        }

        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host)
        {
            _cache ??= new Dictionary<Type, List<IStyle>?>();

            if (_cache.TryGetValue(target.StyleKey, out var cached))
            {
                if (cached is object)
                {
                    foreach (var style in cached)
                    {
                        style.TryAttach(target, host);
                    }

                    return SelectorMatchResult.AlwaysThisType;
                }
                else
                {
                    return SelectorMatchResult.NeverThisType;
                }
            }
            else
            {
                List<IStyle>? matches = null;

                foreach (var child in this)
                {
                    if (child.TryAttach(target, host) != SelectorMatchResult.NeverThisType)
                    {
                        matches ??= new List<IStyle>();
                        matches.Add(child);
                    }
                }

                _cache.Add(target.StyleKey, matches);
                
                return matches is null ?
                    SelectorMatchResult.NeverThisType :
                    SelectorMatchResult.AlwaysThisType;
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object? value)
        {
            if (_resources != null && _resources.TryGetResource(key, out value))
            {
                return true;
            }

            for (var i = Count - 1; i >= 0; --i)
            {
                if (this[i] is IResourceProvider p && p.TryGetResource(key, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <inheritdoc/>
        public void AddRange(IEnumerable<IStyle> items) => _styles.AddRange(items);

        /// <inheritdoc/>
        public void InsertRange(int index, IEnumerable<IStyle> items) => _styles.InsertRange(index, items);

        /// <inheritdoc/>
        public void Move(int oldIndex, int newIndex) => _styles.Move(oldIndex, newIndex);

        /// <inheritdoc/>
        public void MoveRange(int oldIndex, int count, int newIndex) => _styles.MoveRange(oldIndex, count, newIndex);

        /// <inheritdoc/>
        public void RemoveAll(IEnumerable<IStyle> items) => _styles.RemoveAll(items);

        /// <inheritdoc/>
        public void RemoveRange(int index, int count) => _styles.RemoveRange(index, count);

        /// <inheritdoc/>
        public int IndexOf(IStyle item) => _styles.IndexOf(item);

        /// <inheritdoc/>
        public void Insert(int index, IStyle item) => _styles.Insert(index, item);

        /// <inheritdoc/>
        public void RemoveAt(int index) => _styles.RemoveAt(index);

        /// <inheritdoc/>
        public void Add(IStyle item) => _styles.Add(item);

        /// <inheritdoc/>
        public void Clear() => _styles.Clear();

        /// <inheritdoc/>
        public bool Contains(IStyle item) => _styles.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(IStyle[] array, int arrayIndex) => _styles.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public bool Remove(IStyle item) => _styles.Remove(item);

        public AvaloniaList<IStyle>.Enumerator GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator<IStyle> IEnumerable<IStyle>.GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner != null)
            {
                throw new InvalidOperationException("The Styles already has a owner.");
            }

            Owner = owner;
            _resources?.AddOwner(owner);

            foreach (var child in this)
            {
                if (child is IResourceProvider r)
                {
                    r.AddOwner(owner);
                }
            }
        }

        /// <inheritdoc/>
        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner == owner)
            {
                Owner = null;
                _resources?.RemoveOwner(owner);

                foreach (var child in this)
                {
                    if (child is IResourceProvider r)
                    {
                        r.RemoveOwner(owner);
                    }
                }
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            static IReadOnlyList<T> ToReadOnlyList<T>(IList list)
            {
                if (list is IReadOnlyList<T>)
                {
                    return (IReadOnlyList<T>)list;
                }
                else
                {
                    var result = new T[list.Count];
                    list.CopyTo(result, 0);
                    return result;
                }
            }

            void Add(IList items)
            {
                for (var i = 0; i < items.Count; ++i)
                {
                    var style = (IStyle)items[i]!;

                    if (Owner is object && style is IResourceProvider resourceProvider)
                    {
                        resourceProvider.AddOwner(Owner);
                    }

                    _cache = null;
                }

                (Owner as IStyleHost)?.StylesAdded(ToReadOnlyList<IStyle>(items));
            }

            void Remove(IList items)
            {
                for (var i = 0; i < items.Count; ++i)
                {
                    var style = (IStyle)items[i]!;

                    if (Owner is object && style is IResourceProvider resourceProvider)
                    {
                        resourceProvider.RemoveOwner(Owner);
                    }

                    _cache = null;
                }

                (Owner as IStyleHost)?.StylesRemoved(ToReadOnlyList<IStyle>(items));
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldItems!);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldItems!);
                    Add(e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new InvalidOperationException("Reset should not be called on Styles.");
            }

            CollectionChanged?.Invoke(this, e);
        }
    }
