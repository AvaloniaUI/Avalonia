using System;

namespace Avalonia.Controls {

    public interface IViewResolver
    {

        /// <summary>
        /// Resolve type and it dependencies.
        /// </summary>
        /// <param name="type">Type that need receive.</param>
        object Resolve ( Type type );

    }

}