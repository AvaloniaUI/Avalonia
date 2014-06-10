// -----------------------------------------------------------------------
// <copyright file="Direct2DStreamGeometry.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows.Media
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Media;
    using SharpDX.Direct2D1;

    public class Direct2DStreamGeometry : IStreamGeometryImpl
    {
        private PathGeometry geometry;

        public void Initalize(IStreamGeometryImpl impl)
        {
            //this.geometry = new PathGeometry();
        }
    }
}
