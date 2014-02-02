using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex
{
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
    }
}
