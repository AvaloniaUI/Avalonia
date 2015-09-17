using System;
using System.Collections.Generic;
using Perspex;
using Perspex.Controls;

internal class DockingArranger
{
    public Margins Margins { get; private set; }

    public Size ArrangeChildren(Size finalSize, IEnumerable<IControl> controls)
    {
        var leftArranger = new LeftDocker(finalSize);
        var rightArranger = new RightDocker(finalSize);
        var topArranger = new LeftDocker(finalSize.Swap());
        var bottomArranger = new RightDocker(finalSize.Swap());

        Margins = new Margins();

        foreach (var control in controls)
        {
            Rect dockedRect;
            var dock = control.GetValue(DockPanel.DockProperty);
            switch (dock)
            {
                case Dock.Left:
                    dockedRect = leftArranger.GetDockingRect(control.DesiredSize, Margins, control.GetAlignments());
                    break;

                case Dock.Top:
                    Margins.Swap();
                    dockedRect = topArranger.GetDockingRect(control.DesiredSize.Swap(), Margins, control.GetAlignments().Swap()).Swap();
                    Margins.Swap();
                    break;

                case Dock.Right:
                    dockedRect = rightArranger.GetDockingRect(control.DesiredSize, Margins, control.GetAlignments());
                    break;

                case Dock.Bottom:
                    Margins.Swap();
                    dockedRect = bottomArranger.GetDockingRect(control.DesiredSize.Swap(), Margins, control.GetAlignments().Swap()).Swap();
                    Margins.Swap();
                    break;

                default:
                    throw new InvalidOperationException($"Invalid dock value {dock}");
            }

            control.Arrange(dockedRect);
        }

        return finalSize;
    }
}