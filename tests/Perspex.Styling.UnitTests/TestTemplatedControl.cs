// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Perspex.Styling.UnitTests
{
    public abstract class TestTemplatedControl : ITemplatedControl, IStyleable
    {
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

        public abstract IEnumerable<IVisual> VisualChildren
        {
            get;
        }

        public IObservable<T> GetObservable<T>(PerspexProperty<T> property)
        {
            throw new NotImplementedException();
        }

        public IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority)
        {
            throw new NotImplementedException();
        }

        public void SetValue(PerspexProperty property, object value, BindingPriority priority)
        {
            throw new NotImplementedException();
        }

        public IObservable<object> GetObservable(PerspexProperty property)
        {
            throw new NotImplementedException();
        }

        public bool IsRegistered(PerspexProperty property)
        {
            throw new NotImplementedException();
        }

        public void ClearValue(PerspexProperty property)
        {
            throw new NotImplementedException();
        }

        public object GetValue(PerspexProperty property)
        {
            throw new NotImplementedException();
        }

        public bool IsSet(PerspexProperty property)
        {
            throw new NotImplementedException();
        }

        public IDisposable Bind<T>(PerspexProperty<T> property, IObservable<T> source, BindingPriority priority = BindingPriority.LocalValue)
        {
            throw new NotImplementedException();
        }

        public T GetValue<T>(PerspexProperty<T> property)
        {
            throw new NotImplementedException();
        }

        public void SetValue<T>(PerspexProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
        {
            throw new NotImplementedException();
        }
    }
}
