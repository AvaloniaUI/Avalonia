// -----------------------------------------------------------------------
// <copyright file="TestControlBase.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System;
    using Perspex.Styling;

    public class TestControlBase : IStyleable
    {
        public TestControlBase()
        {
            this.Classes = new Classes();
            this.SubscribeCheckObservable = new TestObservable();
        }

        public string Name { get; set; }

        public virtual Classes Classes { get; set; }

        public Type StyleKey
        {
            get { return this.GetType(); }
        }

        public TestObservable SubscribeCheckObservable { get; private set; }

        public ITemplatedControl TemplatedParent
        {
            get;
            set;
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
    }
}
