// -----------------------------------------------------------------------
// <copyright file="VectorEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public class VectorEventArgs : RoutedEventArgs
    {
        public Vector Vector { get; set; }
    }
}
