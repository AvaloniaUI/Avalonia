// -----------------------------------------------------------------------
// <copyright file="StreamGeometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Splat;

    public class StreamGeometry : Geometry
    {
        public StreamGeometry()
        {
            this.Impl = Locator.Current.GetService<IStreamGeometryImpl>();
        }

        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(this);
        }

        internal IStreamGeometryImpl Impl
        {
            get;
            private set;
        }
    }
}
