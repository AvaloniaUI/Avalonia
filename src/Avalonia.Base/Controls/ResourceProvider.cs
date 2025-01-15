using System;
using Avalonia.Styling;

namespace Avalonia.Controls;

/// <summary>
/// Base implementation for IResourceProvider interface.
/// Includes Owner property management.
/// </summary>
public abstract class ResourceProvider : AvaloniaObject, IResourceProvider
{
    private IResourceHost? _owner;

    public ResourceProvider()
    {
    }

    public ResourceProvider(IResourceHost owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    public abstract bool HasResources { get; }

    /// <inheritdoc/>
    public abstract bool TryGetResource(object key, ThemeVariant? theme, out object? value);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public event EventHandler? OwnerChanged;

    protected void RaiseResourcesChanged()
    {
        Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
    }

    /// <summary>
    /// Handles when owner was added.
    /// Base method implementation raises <see cref="IResourceHost.NotifyHostedResourcesChanged"/>, if this provider has any resources.
    /// </summary>
    /// <param name="owner">New owner.</param>
    protected virtual void OnAddOwner(IResourceHost owner)
    {
        if (HasResources)
        {
            owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
        }
    }

    /// <summary>
    /// Handles when owner was removed.
    /// Base method implementation raises <see cref="IResourceHost.NotifyHostedResourcesChanged"/>, if this provider has any resources.
    /// </summary>
    /// <param name="owner">Old owner.</param>
    protected virtual void OnRemoveOwner(IResourceHost owner)
    {
        if (HasResources)
        {
            owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
        }
    }

    void IResourceProvider.AddOwner(IResourceHost owner)
    {
        owner = owner ?? throw new ArgumentNullException(nameof(owner));

        if (Owner != null)
        {
            throw new InvalidOperationException("The ResourceDictionary already has a parent.");
        }

        Owner = owner;

        OnAddOwner(owner);
    }

    void IResourceProvider.RemoveOwner(IResourceHost owner)
    {
        owner = owner ?? throw new ArgumentNullException(nameof(owner));

        if (Owner == owner)
        {
            Owner = null;

            OnRemoveOwner(owner);
        }
    }
}
