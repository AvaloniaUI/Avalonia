using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;

namespace Perspex
{
    public class Match
    {
        public Control Control
        {
            get;
            set;
        }

        public IObservable<bool> Observable
        {
            get;
            set;
        }

        public Match Previous
        {
            get;
            set;
        }
    }
}
