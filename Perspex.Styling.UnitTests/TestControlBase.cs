// -----------------------------------------------------------------------
// <copyright file="SubscribeCheck.cs" company="Steven Kirk">
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

        public string Id { get; set; }

        public virtual Classes Classes { get; set; }

        public TestObservable SubscribeCheckObservable { get; private set; }

        public ITemplatedControl TemplatedParent
        {
            get;
            set;
        }

        public IDisposable Bind(PerspexProperty property, IObservable<object> source, BindingPriority priority = BindingPriority.LocalValue)
        {
            throw new NotImplementedException();
        }
    }
}
