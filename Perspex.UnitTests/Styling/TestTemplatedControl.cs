// -----------------------------------------------------------------------
// <copyright file="SubscribeCheck.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Styling
{
    using System;
    using System.Collections.Generic;
    using Perspex.Controls;
    using Perspex.Styling;

    public abstract class TestTemplatedControl : ITemplatedControl, IStyleable
    {
        public abstract Classes Classes
        {
            get;
        }

        public abstract string Id
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

        public abstract void SetValue(PerspexProperty property, object value, System.IObservable<bool> activator);
    }
}
