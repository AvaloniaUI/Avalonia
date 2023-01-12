using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls.Primitives
{
    public interface IScrollSnapPointsInfo
    {
        bool AreHorizontalSnapPointsRegular { get; }
        bool AreVerticalSnapPointsRegular { get; }

        IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment);
        double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset);

        event EventHandler<RoutedEventArgs> HorizontalSnapPointsChanged;
        event EventHandler<RoutedEventArgs> VerticalSnapPointsChanged;
    }
}
