// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class RadioButton : ToggleButton
    {
        private class RadioButtonGroupManager
        {
            public static readonly RadioButtonGroupManager Default = new RadioButtonGroupManager();
            static readonly List<(WeakReference<IRenderRoot> Root, RadioButtonGroupManager Manager)> s_registeredVisualRoots
                = new List<(WeakReference<IRenderRoot> Root, RadioButtonGroupManager Manager)>();

            readonly Dictionary<string, List<WeakReference<RadioButton>>> s_registeredGroups
                = new Dictionary<string, List<WeakReference<RadioButton>>>();

            public static RadioButtonGroupManager GetOrCreateForRoot(IRenderRoot root)
            {
                if (root == null)
                    return Default;
                lock (s_registeredVisualRoots)
                {
                    int i = 0;
                    while (i < s_registeredVisualRoots.Count)
                    {
                        var item = s_registeredVisualRoots[i].Root;
                        if (!item.TryGetTarget(out var target))
                        {
                            s_registeredVisualRoots.RemoveAt(i);
                            continue;
                        }
                        if (root == target)
                            break;
                        i++;
                    }
                    RadioButtonGroupManager manager;
                    if (i >= s_registeredVisualRoots.Count)
                    {
                        manager = new RadioButtonGroupManager();
                        s_registeredVisualRoots.Add((new WeakReference<IRenderRoot>(root), manager));
                    }
                    else
                    {
                        manager = s_registeredVisualRoots[i].Manager;
                    }
                    return manager;
                }
            }

            public void Add(RadioButton radioButton)
            {
                lock (s_registeredGroups)
                {
                    string groupName = radioButton.GroupName;
                    if (!s_registeredGroups.TryGetValue(groupName, out var group))
                    {
                        group = new List<WeakReference<RadioButton>>();
                        s_registeredGroups.Add(groupName, group);
                    }
                    group.Add(new WeakReference<RadioButton>(radioButton));
                }
            }

            public void Remove(RadioButton radioButton, string oldGroupName)
            {
                lock (s_registeredGroups)
                {
                    if (!string.IsNullOrEmpty(oldGroupName) && s_registeredGroups.TryGetValue(oldGroupName, out var group))
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
                            s_registeredGroups.Remove(oldGroupName);
                        }
                    }
                }
            }

            public void SetChecked(RadioButton radioButton)
            {
                lock (s_registeredGroups)
                {
                    string groupName = radioButton.GroupName;
                    if (s_registeredGroups.TryGetValue(groupName, out var group))
                    {
                        int i = 0;
                        while (i < group.Count)
                        {
                            if (!group[i].TryGetTarget(out var current))
                            {
                                group.RemoveAt(i);
                                continue;
                            }
                            if (current != radioButton && current.IsChecked.GetValueOrDefault())
                                current.IsChecked = false;
                            i++;
                        }
                        if (group.Count == 0)
                        {
                            s_registeredGroups.Remove(groupName);
                        }
                    }
                }
            }
        }

        public static readonly DirectProperty<RadioButton, string> GroupNameProperty =
            AvaloniaProperty.RegisterDirect<RadioButton, string>(
                nameof(GroupName),
                o => o.GroupName,
                (o, v) => o.GroupName = v);

        private string _groupName;
        private RadioButtonGroupManager _groupManager;

        public RadioButton()
        {
            this.GetObservable(IsCheckedProperty).Subscribe(IsCheckedChanged);
        }

        public string GroupName
        {
            get { return _groupName; }
            set { SetGroupName(value); }
        }

        protected override void Toggle()
        {
            if (!IsChecked.GetValueOrDefault())
            {
                IsChecked = true;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (!string.IsNullOrEmpty(GroupName))
            {
                var manager = RadioButtonGroupManager.GetOrCreateForRoot(e.Root);
                if (manager != _groupManager)
                {
                    _groupManager.Remove(this, _groupName);
                    _groupManager = manager;
                    manager.Add(this);
                }
            }
            base.OnAttachedToVisualTree(e);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (!string.IsNullOrEmpty(GroupName) && _groupManager != null)
            {
                _groupManager.Remove(this, _groupName);
            }
        }

        private void SetGroupName(string newGroupName)
        {
            string oldGroupName = GroupName;
            if (newGroupName != oldGroupName)
            {
                if (!string.IsNullOrEmpty(oldGroupName) && _groupManager != null)
                {
                    _groupManager.Remove(this, oldGroupName);
                }
                _groupName = newGroupName;
                if (!string.IsNullOrEmpty(newGroupName))
                {
                    if (_groupManager == null)
                    {
                        _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(this.GetVisualRoot());
                    }
                    _groupManager.Add(this);
                }
            }
        }

        private void IsCheckedChanged(bool? value)
        {
            string groupName = GroupName;
            if (string.IsNullOrEmpty(groupName))
            {
                var parent = this.GetVisualParent();

                if (value.GetValueOrDefault() && parent != null)
                {
                    var siblings = parent
                        .GetVisualChildren()
                        .OfType<RadioButton>()
                        .Where(x => x != this);
                    
                    foreach (var sibling in siblings)
                    {
                        if (sibling.IsChecked.GetValueOrDefault())
                            sibling.IsChecked = false;
                    }
                }
            }
            else
            {
                if (value.GetValueOrDefault() && _groupManager != null)
                {
                    _groupManager.SetChecked(this);
                }
            }
        }
    }
}
