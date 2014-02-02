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

        public string Token
        {
            get;
            set;
        }

        public override string ToString()
        {
            string result = (this.Previous != null) ? this.Previous.ToString() : string.Empty;
            result += this.Token;
            return result;
        }
    }
}
