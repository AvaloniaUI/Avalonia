using System.Collections.Generic;
using Avalonia.Controls;

namespace Avalonia.Benchmarks
{
    internal class ControlHierarchyCreator
    {
        public static List<Control> CreateChildren(List<Control> controls, Panel parent, int childCount, int innerCount, int iterations)
        {
            for (var i = 0; i < childCount; ++i)
            {
                var control = new StackPanel();
                parent.Children.Add(control);

                for (int j = 0; j < innerCount; ++j)
                {
                    var child = new Button();

                    parent.Children.Add(child);

                    controls.Add(child);
                }

                if (iterations > 0)
                {
                    CreateChildren(controls, control, childCount, innerCount, iterations - 1);
                }

                controls.Add(control);
            }

            return controls;
        }
    }
}
