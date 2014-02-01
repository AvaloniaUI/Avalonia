using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;

namespace Perspex
{
    public class Style
    {
        public Style()
        {
            this.Setters = new List<Setter>();
        }

        public Func<Control, IObservable<bool>> Selector
        {
            get;
            set;
        }

        public IEnumerable<Setter> Setters
        {
            get;
            set;
        }

        public void Attach(Control control)
        {
            this.Selector(control).Subscribe(x => 
            {
                if (x)
                {
                    this.Apply(control);
                }
                else
                {
                    this.Unapply(control);
                }
            });
        }

        private void Apply(Control control)
        {
            if (this.Setters != null)
            {
                foreach (Setter setter in this.Setters)
                {
                    setter.Apply(control);
                }
            }
        }

        private void Unapply(Control control)
        {
            if (this.Setters != null)
            {
                foreach (Setter setter in this.Setters)
                {
                    setter.Detach(control);
                }
            }
        }
    }
}
