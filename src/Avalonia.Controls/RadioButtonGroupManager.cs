using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Rendering;

namespace Avalonia.Controls;

internal interface IGroupRadioButton : ILogical
{
    string? GroupName { get; }
    bool IsChecked { get; set; }
    void SubscribeOnChecked(Action<IGroupRadioButton> action);
}

internal class RadioButtonGroupManager
{
    private static readonly RadioButtonGroupManager s_default = new();
    private static readonly ConditionalWeakTable<IRenderRoot, RadioButtonGroupManager> s_registeredVisualRoots = new();

    private readonly Dictionary<string, List<WeakReference<IGroupRadioButton>>> _registeredGroups = new();
    private bool _ignoreCheckedChanges;

    public static RadioButtonGroupManager GetOrCreateForRoot(IRenderRoot? root)
    {
        if (root == null)
            return s_default;
        return s_registeredVisualRoots.GetValue(root, key => new RadioButtonGroupManager());
    }

    public void Add(IGroupRadioButton radioButton)
    {
        var groupName = radioButton.GroupName;
        if (groupName is not null)
        {
            if (!_registeredGroups.TryGetValue(groupName, out var group))
            {
                group = new List<WeakReference<IGroupRadioButton>>();
                _registeredGroups.Add(groupName, group);
            }

            group.Add(new WeakReference<IGroupRadioButton>(radioButton));
        }

        radioButton.SubscribeOnChecked(OnCheckedChanged);
    }

    public void Remove(IGroupRadioButton radioButton, string? oldGroupName)
    {
        if (!string.IsNullOrEmpty(oldGroupName) && _registeredGroups.TryGetValue(oldGroupName, out var group))
        {
            int i = 0;
            while (i < group.Count)
            {
                if (!group[i].TryGetTarget(out var button) || button == radioButton)
                {
                    group.RemoveAt(i);
                    continue;
                }

                i++;
            }

            if (group.Count == 0)
            {
                _registeredGroups.Remove(oldGroupName);
            }
        }
    }

    public void OnCheckedChanged(IGroupRadioButton radioButton)
    {
        if (_ignoreCheckedChanges)
        {
            return;
        }

        _ignoreCheckedChanges = true;
        try
        {
            if (radioButton.GroupName is { Length: > 0 } groupName)
            {
                if (_registeredGroups.TryGetValue(groupName, out var group))
                {
                    var i = 0;
                    while (i < group.Count)
                    {
                        if (!group[i].TryGetTarget(out var current))
                        {
                            group.RemoveAt(i);
                            continue;
                        }

                        if (current != radioButton && current.IsChecked)
                            current.IsChecked = false;
                        i++;
                    }

                    if (group.Count == 0)
                    {
                        _registeredGroups.Remove(groupName);
                    }

                    var parent = radioButton.LogicalParent as IGroupRadioButton;
                    while (parent is not null && parent.GroupName == groupName)
                    {
                        parent.IsChecked = true;
                        parent = parent.LogicalParent as IGroupRadioButton;
                    }
                }
            }
            else
            {
                if (radioButton.LogicalParent is { } parent)
                {
                    foreach (var sibling in parent.LogicalChildren)
                    {
                        if (sibling != radioButton && sibling is IGroupRadioButton { IsChecked: true } button)
                        {
                            button.IsChecked = false;
                        }
                    }
                }
            }
        }
        finally
        {
            _ignoreCheckedChanges = false;
        }
    }
}
