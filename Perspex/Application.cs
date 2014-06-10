// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System.Reflection;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Styling;
    using Splat;

    public class Application
    {
        private Styles styles;

        public Application()
        {
            Current = this;
        }

        public static Application Current
        {
            get;
            private set;
        }

        public Styles Styles
        {
            get
            {
                if (this.styles == null)
                {
                    this.styles = new Styles();
                }

                return this.styles;
            }

            set
            {
                this.styles = value;
            }
        }

        public static void RegisterPortableServices()
        {
            InputManager inputManager = new InputManager();
            Locator.CurrentMutable.Register(() => inputManager, typeof(IInputManager));
        }
    }
}
