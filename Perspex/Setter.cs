// -----------------------------------------------------------------------
// <copyright file="Setter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using Perspex.Controls;

    public class Setter
    {
        public Setter()
        {
        }

        public Setter(PerspexProperty property, object value)
        {
            this.Property = property;
            this.Value = value;
        }

        public PerspexProperty Property
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }
    }
}
