using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Controls;

internal class TopLevelGroup : AvaloniaObject
{
    public TopLevelGroup(IDevToolsTopLevelGroup group)
    {
        Group = group;
        if (Group.Items is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += (_, args) =>
            {
                if (args.OldItems is not null)
                {
                    foreach (TopLevel oldItem in args.OldItems)
                    {
                        Removed?.Invoke(this, oldItem);
                    }
                }

                if (args.NewItems is not null)
                {
                    foreach (TopLevel newItem in args.NewItems)
                    {
                        Added?.Invoke(this, newItem);
                    }
                }
            };
        }
    }

    public IDevToolsTopLevelGroup Group { get; }

    public IReadOnlyList<TopLevel> Items => Group.Items;
    public event EventHandler<TopLevel>? Added;
    public event EventHandler<TopLevel>? Removed;
}
