using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a button that allows a user to select a single option from a group of options.
    /// </summary>
    public class RadioButton : ToggleButton
    {
        private class RadioButtonGroupManager
        {
            public static readonly RadioButtonGroupManager Default = new RadioButtonGroupManager();
            static readonly ConditionalWeakTable<IRenderRoot, RadioButtonGroupManager> s_registeredVisualRoots
                = new ConditionalWeakTable<IRenderRoot, RadioButtonGroupManager>();

            readonly Dictionary<string, List<WeakReference<RadioButton>>> s_registeredGroups
                = new Dictionary<string, List<WeakReference<RadioButton>>>();

            public static RadioButtonGroupManager GetOrCreateForRoot(IRenderRoot? root)
            {
                if (root == null)
                    return Default;
                return s_registeredVisualRoots.GetValue(root, key => new RadioButtonGroupManager());
            }

            public void Add(RadioButton radioButton)
            {
                lock (s_registeredGroups)
                {
                    string groupName = radioButton.GroupName!;
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
                    string groupName = radioButton.GroupName!;
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

        public static readonly StyledProperty<string?> GroupNameProperty =
            AvaloniaProperty.Register<RadioButton, string?>(nameof(GroupName));

        private RadioButtonGroupManager? _groupManager;

        public string? GroupName
        {
            get => GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        protected override void Toggle()
        {
            if (!IsChecked.GetValueOrDefault())
            {
                SetCurrentValue(IsCheckedProperty, true);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (!string.IsNullOrEmpty(GroupName))
            {
                _groupManager?.Remove(this, GroupName);

                _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(e.Root);

                _groupManager.Add(this);
            }
            base.OnAttachedToVisualTree(e);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (!string.IsNullOrEmpty(GroupName))
            {
                _groupManager?.Remove(this, GroupName);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RadioButtonAutomationPeer(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsCheckedProperty)
            {
                IsCheckedChanged(change.GetNewValue<bool?>());
            }
            else if (change.Property == GroupNameProperty)
            {
                var (oldValue, newValue) = change.GetOldAndNewValue<string?>();
                OnGroupNameChanged(oldValue, newValue);
            }
        }

        private void OnGroupNameChanged(string? oldGroupName, string? newGroupName)
        {
            if (!string.IsNullOrEmpty(oldGroupName))
            {
                _groupManager?.Remove(this, oldGroupName);
            }
            if (!string.IsNullOrEmpty(newGroupName))
            {
                if (_groupManager == null)
                {
                    _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(this.GetVisualRoot());
                }
                _groupManager.Add(this);
            }
        }

        private new void IsCheckedChanged(bool? value)
        {
            var groupName = GroupName;
            if (string.IsNullOrEmpty(groupName))
            {
                var parent = this.GetVisualParent();

                if (value.GetValueOrDefault() && parent != null)
                {
                    var siblings = parent
                        .GetVisualChildren()
                        .OfType<RadioButton>()
                        .Where(x => x != this && string.IsNullOrEmpty(x.GroupName));

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
