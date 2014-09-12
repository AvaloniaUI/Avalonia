using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            builder.Append(Indent(indent - 1));

            if (indent > 0)
            {
                builder.Append(" +- ");
            }

            builder.AppendLine(visual.GetType().Name);

            PerspexObject p = visual as PerspexObject;

            if (p != null)
            {
                foreach (var value in p.GetSetValues())
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
