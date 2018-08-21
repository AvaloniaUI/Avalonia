// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class RadioButton : ToggleButton
    {
        public static readonly DirectProperty<RadioButton, string> GroupNameProperty =
            AvaloniaProperty.RegisterDirect<RadioButton, string>(
                nameof(GroupName),
                o => o.GroupName,
                (o, v) => o.GroupName = v);

        private string _groupName;

        [ThreadStatic]
        private static readonly Dictionary<string, List<WeakReference<RadioButton>>> s_registeredGroups;

        static RadioButton()
        {
            s_registeredGroups = new Dictionary<string, List<WeakReference<RadioButton>>>();
        }

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

        private void SetGroupName(string newGroupName)
        {
            string oldGroupName = GroupName;
            if (newGroupName != oldGroupName)
            {
                lock (s_registeredGroups)
                {
                    if (!string.IsNullOrEmpty(oldGroupName) && s_registeredGroups.TryGetValue(oldGroupName, out var group))
                    {
                        int i = 0;
                        while (i < group.Count)
                        {
                            if (!group[i].TryGetTarget(out var button) || button == this)
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
                    if (!string.IsNullOrEmpty(newGroupName))
                    {
                        if (!s_registeredGroups.TryGetValue(newGroupName, out group))
                        {
                            group = new List<WeakReference<RadioButton>>();
                            s_registeredGroups.Add(newGroupName, group);
                        }
                        group.Add(new WeakReference<RadioButton>(this));
                    }
                }
                _groupName = newGroupName;
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
                lock (s_registeredGroups)
                {
                    if (value.GetValueOrDefault() && s_registeredGroups.TryGetValue(groupName, out var group))
                    {
                        int i = 0;
                        while (i < group.Count)
                        {
                            if (!group[i].TryGetTarget(out var sibling))
                            {
                                group.RemoveAt(i);
                            }
                            else
                            {
                                if (sibling != this && sibling.IsChecked.GetValueOrDefault())
                                    sibling.IsChecked = false;
                                i++;
                            }
                        }
                    }
                }
            }
        }
    }
}
