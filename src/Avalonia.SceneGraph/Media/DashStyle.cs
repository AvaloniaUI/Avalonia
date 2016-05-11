namespace Avalonia.Media
{
    using Avalonia.Animation;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DashStyle : Animatable
    {
        private static DashStyle dash;
        public static DashStyle Dash
        {
            get
            {
                if (dashDotDot == null)
                {
                    dash = new DashStyle(new double[] { 2, 2 }, 1);
                }

                return dash;
            }
        }



        private static DashStyle dot;
        public static DashStyle Dot
        {
            get { return dot ?? (dot = new DashStyle(new double[] {0, 2}, 0)); }
        }

        private static DashStyle dashDot;
        public static DashStyle DashDot
        {
            get
            {
                if (dashDot == null)
                {
                    dashDot = new DashStyle(new double[] { 2, 2, 0, 2 }, 1);
                }

                return dashDot;
            }
        }

        private static DashStyle dashDotDot;
        public static DashStyle DashDotDot
        {
            get
            {
                if (dashDotDot == null)
                {
                    dashDotDot = new DashStyle(new double[] { 2, 2, 0, 2, 0, 2 }, 1);
                }

                return dashDotDot;
            }
        }


        public DashStyle(IReadOnlyList<double> dashes = null, double offset = 0.0)
        {
            this.Dashes = dashes;
            this.Offset = offset;
        }

        /// <summary>
        /// Gets and sets the length of alternating dashes and gaps.
        /// </summary>
        public IReadOnlyList<double> Dashes { get; }

        public double Offset { get; }
    }
}
