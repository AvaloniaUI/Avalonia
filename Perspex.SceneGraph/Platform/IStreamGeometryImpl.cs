// -----------------------------------------------------------------------
// <copyright file="IStreamGeometryImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    public interface IStreamGeometryImpl : IGeometryImpl
    {
        IStreamGeometryImpl Clone();

        IStreamGeometryContextImpl Open();
    }
}
