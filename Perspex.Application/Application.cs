// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Threading;
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Styling;
    using Perspex.Threading;
    using Splat;

    public class Application : IGlobalDataTemplates, IGlobalStyles
    {
        private DataTemplates dataTemplates;

        private Styler styler = new Styler();

        public Application(Styles theme)
        {
            if (Current != null)
            {
                throw new InvalidOperationException("Cannot create more than one Application instance.");
            }

            Current = this;
            this.Styles = theme;
            this.FocusManager = new FocusManager();
            this.InputManager = new InputManager();
            this.RegisterServices();
        }

        public static Application Current
        {
            get;
            private set;
        }

        public DataTemplates DataTemplates
        {
            get
            {
                if (this.dataTemplates == null)
                {
                    this.dataTemplates = new DataTemplates();
                }

                return this.dataTemplates;
            }

            set
            {
                this.dataTemplates = value;
            }
        }

        public IFocusManager FocusManager
        {
            get;
            private set;
        }

        public InputManager InputManager
        {
            get;
            private set;
        }

        public Styles Styles
        {
            get;
            private set;
        }

        public void Run(ICloseable closable)
        {
            var source = new CancellationTokenSource();
            closable.Closed += (s, e) => source.Cancel();
            Dispatcher.UIThread.MainLoop(source.Token);
        }

        protected virtual void RegisterServices()
        {
            Locator.CurrentMutable.Register(() => this, typeof(IGlobalDataTemplates));
            Locator.CurrentMutable.Register(() => this, typeof(IGlobalStyles));
            Locator.CurrentMutable.Register(() => this.FocusManager, typeof(IFocusManager));
            Locator.CurrentMutable.Register(() => this.InputManager, typeof(IInputManager));
            Locator.CurrentMutable.Register(() => this.styler, typeof(IStyler));
        }
    }
}
