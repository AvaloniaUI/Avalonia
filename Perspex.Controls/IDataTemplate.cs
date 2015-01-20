// -----------------------------------------------------------------------
// <copyright file="IDataTemplate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public interface IDataTemplate
    {
        bool Match(object data);

        Control Build(object data);
    }
}
