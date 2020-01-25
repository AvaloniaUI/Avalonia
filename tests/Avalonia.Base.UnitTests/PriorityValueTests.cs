﻿using System;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class PriorityValueTests
    {
        private static readonly IValueSink NullSink = Mock.Of<IValueSink>();
        private static readonly IAvaloniaObject Owner = Mock.Of<IAvaloniaObject>();
        private static readonly StyledProperty<string> TestProperty = new StyledProperty<string>(
            "Test",
            typeof(PriorityValueTests),
            new StyledPropertyMetadata<string>());

        [Fact]
        public void Constructor_Should_Set_Value_Based_On_Initial_Entry()
        {
            var target = new PriorityValue<string>(
                Owner,
                TestProperty,
                NullSink,
                new ConstantValueEntry<string>(TestProperty, "1", BindingPriority.StyleTrigger));

            Assert.Equal("1", target.Value.Value);
            Assert.Equal(BindingPriority.StyleTrigger, target.ValuePriority);
        }

        [Fact]
        public void SetValue_LocalValue_Should_Not_Add_Entries()
        {
            var target = new PriorityValue<string>(
                Owner,
                TestProperty,
                NullSink);

            target.SetValue("1", BindingPriority.LocalValue);
            target.SetValue("2", BindingPriority.LocalValue);

            Assert.Empty(target.Entries);
        }

        [Fact]
        public void SetValue_Non_LocalValue_Should_Add_Entries()
        {
            var target = new PriorityValue<string>(
                Owner,
                TestProperty,
                NullSink);

            target.SetValue("1", BindingPriority.Style);
            target.SetValue("2", BindingPriority.Animation);

            var result = target.Entries
                .OfType<ConstantValueEntry<string>>()
                .Select(x => x.Value.Value)
                .ToList();

            Assert.Equal(new[] { "1", "2" }, result);
        }

        [Fact]
        public void Binding_With_Same_Priority_Should_Be_Appended()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");
            var source2 = new Source("2");

            target.AddBinding(source1, BindingPriority.LocalValue);
            target.AddBinding(source2, BindingPriority.LocalValue);

            var result = target.Entries
                .OfType<BindingEntry<string>>()
                .Select(x => x.Source)
                .OfType<Source>()
                .Select(x => x.Id)
                .ToList();

            Assert.Equal(new[] { "1", "2" }, result);
        }

        [Fact]
        public void Binding_With_Higher_Priority_Should_Be_Appended()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");
            var source2 = new Source("2");

            target.AddBinding(source1, BindingPriority.LocalValue);
            target.AddBinding(source2, BindingPriority.Animation);

            var result = target.Entries
                .OfType<BindingEntry<string>>()
                .Select(x => x.Source)
                .OfType<Source>()
                .Select(x => x.Id)
                .ToList();

            Assert.Equal(new[] { "1", "2" }, result);
        }

        [Fact]
        public void Binding_With_Lower_Priority_Should_Be_Prepended()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");
            var source2 = new Source("2");

            target.AddBinding(source1, BindingPriority.LocalValue);
            target.AddBinding(source2, BindingPriority.Style);

            var result = target.Entries
                .OfType<BindingEntry<string>>()
                .Select(x => x.Source)
                .OfType<Source>()
                .Select(x => x.Id)
                .ToList();

            Assert.Equal(new[] { "2", "1" }, result);
        }

        [Fact]
        public void Second_Binding_With_Lower_Priority_Should_Be_Inserted_In_Middle()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");
            var source2 = new Source("2");
            var source3 = new Source("3");

            target.AddBinding(source1, BindingPriority.LocalValue);
            target.AddBinding(source2, BindingPriority.Style);
            target.AddBinding(source3, BindingPriority.Style);

            var result = target.Entries
                .OfType<BindingEntry<string>>()
                .Select(x => x.Source)
                .OfType<Source>()
                .Select(x => x.Id)
                .ToList();

            Assert.Equal(new[] { "2", "3", "1" }, result);
        }

        [Fact]
        public void Competed_Binding_Should_Be_Removed()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");
            var source2 = new Source("2");
            var source3 = new Source("3");

            target.AddBinding(source1, BindingPriority.LocalValue).Start();
            target.AddBinding(source2, BindingPriority.Style).Start();
            target.AddBinding(source3, BindingPriority.Style).Start();
            source3.OnCompleted();

            var result = target.Entries
                .OfType<BindingEntry<string>>()
                .Select(x => x.Source)
                .OfType<Source>()
                .Select(x => x.Id)
                .ToList();

            Assert.Equal(new[] { "2", "1" }, result);
        }

        [Fact]
        public void Value_Should_Come_From_Last_Entry()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");
            var source2 = new Source("2");
            var source3 = new Source("3");

            target.AddBinding(source1, BindingPriority.LocalValue).Start();
            target.AddBinding(source2, BindingPriority.Style).Start();
            target.AddBinding(source3, BindingPriority.Style).Start();

            Assert.Equal("1", target.Value.Value);
        }

        [Fact]
        public void LocalValue_Should_Override_LocalValue_Binding()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");

            target.AddBinding(source1, BindingPriority.LocalValue).Start();
            target.SetValue("2", BindingPriority.LocalValue);

            Assert.Equal("2", target.Value.Value);
        }

        [Fact]
        public void LocalValue_Should_Override_Style_Binding()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");

            target.AddBinding(source1, BindingPriority.Style).Start();
            target.SetValue("2", BindingPriority.LocalValue);

            Assert.Equal("2", target.Value.Value);
        }

        [Fact]
        public void LocalValue_Should_Not_Override_Animation_Binding()
        {
            var target = new PriorityValue<string>(Owner, TestProperty, NullSink);
            var source1 = new Source("1");

            target.AddBinding(source1, BindingPriority.Animation).Start();
            target.SetValue("2", BindingPriority.LocalValue);

            Assert.Equal("1", target.Value.Value);
        }

        private class Source : IObservable<BindingValue<string>>
        {
            private IObserver<BindingValue<string>> _observer;

            public Source(string id) => Id = id;
            public string Id { get; }

            public IDisposable Subscribe(IObserver<BindingValue<string>> observer)
            {
                _observer = observer;
                observer.OnNext(Id);
                return Disposable.Empty;
            }

            public void OnCompleted() => _observer.OnCompleted();
        }
    }
}
