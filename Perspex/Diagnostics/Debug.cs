using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;

namespace Perspex.Diagnostics
{
    public static class Debug
    {
        public static string PrintVisualTree(IVisual visual)
        {
            StringBuilder result = new StringBuilder();
            PrintVisualTree(visual, result, 0);
            return result.ToString();
        }

        private static void PrintVisualTree(IVisual visual, StringBuilder builder, int indent)
        {
            Control control = visual as Control;

            builder.Append(Indent(indent - 1));

            if (indent > 0)
            {
                builder.Append(" +- ");
            }

            builder.Append(visual.GetType().Name);

            if (control != null)
            {
                builder.Append(" ");
                builder.AppendLine(control.Classes.ToString());

                foreach (var value in control.GetSetValues())
                {
                    builder.Append(Indent(indent));
                    builder.Append(" |  ");
                    builder.Append(value.Item1.Name);
                    builder.Append(" = ");
                    builder.Append(value.Item2 ?? "(null)");
                    builder.Append(" [");
                    builder.Append(value.Item3);
                    builder.AppendLine("]");
                }
            }
            else
            {
                builder.AppendLine();
            }

            foreach (var child in visual.VisualChildren)
            {
                PrintVisualTree(child, builder, indent + 1);
            }
        }

        private static string Indent(int indent)
        {
            return string.Join("", Enumerable.Repeat("    ", Math.Max(indent, 0)));
        }
    }
}
