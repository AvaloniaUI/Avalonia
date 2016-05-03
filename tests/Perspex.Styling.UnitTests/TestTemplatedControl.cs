// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Data;

namespace Perspex.Styling.UnitTests
{
    public abstract class TestTemplatedControl : ITemplatedControl, IStyleable
    {
        public event EventHandler<PerspexPropertyChangedEventArgs> PropertyChanged;

        public abstract Classes Classes
        {
            get;
        }

        public abstract string Name
        {
            get;
        }

        public abstract Type StyleKey
        {
            get;
        }

        public abstract ITemplatedControl TemplatedParent
        {
            get;
        }

        IPerspexReadOnlyList<string> IStyleable.Classes => Classes;

        IObservable<IStyleable> IStyleable.StyleDetach { get; }

        public object GetValue(PerspexProperty property)
        {
            throw new NotImplementedException();
        }

        public T GetValue<T>(PerspexProperty<T> property)
        {
            throw new NotImplementedException();
        }

        public void SetValue(PerspexProperty property, object value, BindingPriority priority)
        {
            throw new NotImplementedException();
        }

        public void SetValue<T>(PerspexProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
        {
            throw new NotImplementedException();
        }

        public IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority = BindingPriority.LocalValue)
        {
            throw new NotImplementedException();
        }

        public IDisposable Bind<T>(PerspexProperty<T> property, IObservable<T> source, BindingPriority priority = BindingPriority.LocalValue)
        {
            throw new NotImplementedException();
        }

        public bool IsSet(PerspexProperty property)
        {
            throw new NotImplementedException();
        }
    }
}
