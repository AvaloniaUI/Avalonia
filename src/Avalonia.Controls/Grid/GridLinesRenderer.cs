// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Avalonia.VisualTree;
using Avalonia.Media;

namespace Avalonia.Controls
{
    internal class GridLinesRenderer : Control
    {
        /// <summary>
        /// Static initialization
        /// </summary>
        static GridLinesRenderer()
        {
            var oddDashArray = new List<double>();
            oddDashArray.Add(_dashLength);
            oddDashArray.Add(_dashLength);
            var ds1 = new DashStyle(oddDashArray, 0);
            _oddDashPen = new Pen(Brushes.Blue,
                                   _penWidth,
                                   lineCap: PenLineCap.Flat,
                                   dashStyle: ds1);

            var evenDashArray = new List<double>();
            evenDashArray.Add(_dashLength);
            evenDashArray.Add(_dashLength);
            var ds2 = new DashStyle(evenDashArray, 0);
            _evenDashPen = new Pen(Brushes.Yellow,
                                   _penWidth,
                                   lineCap: PenLineCap.Flat,
                                   dashStyle: ds2);
        }

        /// <summary>
        /// UpdateRenderBounds.
        /// </summary>
        public override void Render(DrawingContext drawingContext)
        {
            var grid = this.GetVisualParent<Grid>();

            if (grid == null
                || !grid.ShowGridLines
                || grid.IsTrivialGrid)
            {
                return;
            }

            for (int i = 1; i < grid.ColumnDefinitions.Count; ++i)
            {
                DrawGridLine(
                    drawingContext,
                    grid.ColumnDefinitions[i].FinalOffset, 0.0,
                    grid.ColumnDefinitions[i].FinalOffset, _lastArrangeSize.Height);
            }

            for (int i = 1; i < grid.RowDefinitions.Count; ++i)
            {
                DrawGridLine(
                    drawingContext,
                    0.0, grid.RowDefinitions[i].FinalOffset,
                    _lastArrangeSize.Width, grid.RowDefinitions[i].FinalOffset);
            }
        }

        /// <summary>
        /// Draw single hi-contrast line.
        /// </summary>
        private static void DrawGridLine(
            DrawingContext drawingContext,
            double startX,
            double startY,
            double endX,
            double endY)
        {
            var start = new Point(startX, startY);
            var end = new Point(endX, endY);
            drawingContext.DrawLine(_oddDashPen, start, end);
            drawingContext.DrawLine(_evenDashPen, start, end);
        }

        internal void UpdateRenderBounds(Size arrangeSize)
        {
            _lastArrangeSize = arrangeSize;
            this.InvalidateVisual();
        }

        private static Size _lastArrangeSize;
        private const double _dashLength = 4.0;    //
        private const double _penWidth = 1.0;      //
        private static readonly Pen _oddDashPen;   //  first pen to draw dash
        private static readonly Pen _evenDashPen;  //  second pen to draw dash
    }
}