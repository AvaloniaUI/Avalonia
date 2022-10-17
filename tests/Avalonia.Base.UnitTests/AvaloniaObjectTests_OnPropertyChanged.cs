// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_OnPropertyChanged
    {
        [Fact]
        public void OnPropertyChangedCore_Is_Called_On_Property_Change()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal(1, target.CoreChanges.Count);

            var change = (AvaloniaPropertyChangedEventArgs<string>)target.CoreChanges[0];

            Assert.Equal("newvalue", change.NewValue.Value);
            Assert.Equal("foodefault", change.OldValue.Value);
            Assert.Equal(BindingPriority.LocalValue, change.Priority);
            Assert.True(change.IsEffectiveValueChange);
        }

        [Fact]
        public void OnPropertyChangedCore_Is_Called_On_Non_Effective_Property_Value_Change()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Animation);
            target.SetValue(Class1.FooProperty, "styled", BindingPriority.Style);

            Assert.Equal(2, target.CoreChanges.Count);

            var change = (AvaloniaPropertyChangedEventArgs<string>)target.CoreChanges[1];

            Assert.Equal("styled", change.NewValue.Value);
            Assert.False(change.OldValue.HasValue);
            Assert.Equal(BindingPriority.Style, change.Priority);
            Assert.False(change.IsEffectiveValueChange);
        }

        [Fact]
        public void OnPropertyChangedCore_Is_Called_On_Non_Effective_Property_Binding_Value_Change()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("styled1");

            target.Bind(Class1.FooProperty, source, BindingPriority.Style);
            target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Animation);
            source.OnNext("styled2");

            Assert.Equal(3, target.CoreChanges.Count);

            var change = (AvaloniaPropertyChangedEventArgs<string>)target.CoreChanges[2];
            
            Assert.Equal("styled2", change.NewValue.Value);
            Assert.False(change.OldValue.HasValue);
            Assert.Equal(BindingPriority.Style, change.Priority);
            Assert.False(change.IsEffectiveValueChange);
        }

        [Fact]
        public void OnPropertyChanged_Is_Called_Only_For_Effective_Value_Changes()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Animation);
            target.SetValue(Class1.FooProperty, "styled", BindingPriority.Style);

            Assert.Equal(1, target.Changes.Count);
            Assert.Equal(2, target.CoreChanges.Count);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public Class1()
            {
                Changes = new List<AvaloniaPropertyChangedEventArgs>();
                CoreChanges = new List<AvaloniaPropertyChangedEventArgs>();
            }

            public List<AvaloniaPropertyChangedEventArgs> Changes { get; }
            public List<AvaloniaPropertyChangedEventArgs> CoreChanges { get; }

            protected override void OnPropertyChangedCore(AvaloniaPropertyChangedEventArgs change)
            {
                CoreChanges.Add(Clone(change));
                base.OnPropertyChangedCore(change);
            }

            protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
            {
                Changes.Add(Clone(change));
                base.OnPropertyChanged(change);
            }

            private static AvaloniaPropertyChangedEventArgs Clone(AvaloniaPropertyChangedEventArgs change)
            {
                var e = (AvaloniaPropertyChangedEventArgs<string>)change;
                return new AvaloniaPropertyChangedEventArgs<string>(
                    change.Sender,
                    e.Property,
                    e.OldValue,
                    e.NewValue,
                    change.Priority,
                    change.IsEffectiveValueChange);
            }
        }
    }
}
