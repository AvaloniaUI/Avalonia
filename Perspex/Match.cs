// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using Perspex.Controls;

    public class Match
    {
        public Control Control
        {
            get;
            set;
        }

        public IObservable<bool> Observable
        {
            get;
            set;
        }

        public Match Previous
        {
            get;
            set;
        }
    }
}
