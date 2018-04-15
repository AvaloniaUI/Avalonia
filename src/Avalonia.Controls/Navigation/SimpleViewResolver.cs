using System;

namespace Avalonia.Controls {

    public class SimpleViewResolver : IViewResolver {

        public object Resolve ( Type type ) {
            return Activator.CreateInstance ( type );
        }

    }

}