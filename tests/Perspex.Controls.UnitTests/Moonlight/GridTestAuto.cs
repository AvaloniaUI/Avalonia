using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls.Shapes;
using Perspex.Layout;
using Perspex.Media;

using FrameworkElement = Perspex.Controls.Control;
using UIElement = Perspex.Controls.Control;

namespace Perspex.Controls.UnitTests.Moonlight
{
    static class GridExtensions
	{
		public static void ResetAndInvalidate (this MyGrid grid)
		{
			grid.Reset ();
			grid.InvalidateSubtree ();
		}
		
		// This method checks all the children of the grid to ensure that the last time they were
		// measured, they were passed the correct Size arg.
		public static void CheckFinalMeasureArg (this MyGrid grid, string message, params Size [] sizes)
		{
			Assert.AreEqual (sizes.Length, grid.Children.Count, message + " : Incorrect number of sizes passed");
			for (int i = 0; i < sizes.Length; i++)
				Assert.AreEqual (sizes [i], ((MyContentControl) grid.Children [i]).MeasureOverrideArg, message + "." + i);
		}
		public static void AddChild (this Grid grid, FrameworkElement element, int row, int column, int rowspan, int columnspan)
		{
			if (row != -1)
				Grid.SetRow (element, row);
			if (rowspan != 0)
				Grid.SetRowSpan (element, rowspan);
			if (column != -1)
				Grid.SetColumn (element, column);
			if (columnspan != 0)
				Grid.SetColumnSpan (element, columnspan);
			grid.Children.Add (element);
		}

		public static void AddColumns (this Grid grid, params GridLength [] columns)
		{
			foreach (GridLength c in columns)
				grid.ColumnDefinitions.Add (new ColumnDefinition { Width = c });
		}

		public static void AddRows (this Grid grid, params GridLength [] rows)
		{
			foreach (GridLength c in rows)
				grid.RowDefinitions.Add (new RowDefinition { Height = c });
		}

		public static void ChangeRow (this Grid grid, int childIndex, int newRow)
		{
			Grid.SetRow ((FrameworkElement) grid.Children [childIndex], newRow);
		}

		public static void ChangeRowSpan (this Grid grid, int childIndex, int newRowSpan)
		{
			Grid.SetRowSpan ((FrameworkElement) grid.Children [childIndex], newRowSpan);
		}

		public static void ChangeCol (this Grid grid, int childIndex, int newCol)
		{
			Grid.SetColumn ((FrameworkElement) grid.Children [childIndex], newCol);
		}

		public static void ChangeColSpan (this Grid grid, int childIndex, int newColSpan)
		{
			Grid.SetColumnSpan ((FrameworkElement) grid.Children [childIndex], newColSpan);
		}

		// Checks that the desired size of all the children are correct.
		public static void CheckDesired (this Grid grid, string message, params Size [] sizes)
		{
			for (int i = 0; i < grid.Children.Count; i++) {
				var poker = (MyContentControl) grid.Children [i];
				if (!poker.DesiredSize.Equals (sizes [i]))
					Assert.Fail ("{2}.{3} Expected measure result to be {0} but was {1}", sizes [i], poker.DesiredSize, message, i);
			}
		}

		// Checks that the ActualWidth of the grid Columns are correct
		public static void CheckColWidths (this Grid grid, string message, params double [] widths)
		{
			for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
				Assert.IsBetween (widths [i] - 0.55, widths [i] + 0.55, grid.ColumnDefinitions [i].ActualWidth, message + "." + i);
		}

		// Checks that the ActualHeight of the grid Rows are correct
		public static void CheckRowHeights (this Grid grid, string message, params double [] heights)
		{
			for (int i = 0; i < grid.RowDefinitions.Count; i++)
				Assert.IsBetween (heights [i] - 0.55, heights [i] + 0.55, grid.RowDefinitions [i].ActualHeight, message + "." + i);
		}

		// Every time an element in the grid is measured, it adds itself and its Size argument to the
		// MeasuredElements list. This way we can see which elements are measured multiple times and also
		// the order in which elements are measured. This helper method just checks that all the MeasureOverride args
		// are correct.
		public static void CheckMeasureArgs (this MyGrid grid, string message, params Size [] sizes)
		{
			Assert.AreEqual (sizes.Length, grid.MeasuredElements.Count, "Wrong number of elements were measured. {0}", message);
			for (int i = 0; i < grid.MeasuredElements.Count; i++) {
				try {
					Assert.IsBetween (sizes [i].Height - 0.55, sizes [i].Height + 0.55, grid.MeasuredElements [i].Value.Height, "#1");
					Assert.IsBetween (sizes [i].Width - 0.55, sizes [i].Width + 0.55, grid.MeasuredElements [i].Value.Width, "#1");
				} catch {
					Assert.Fail ("{2}.{3} Expected measure argument to be {0} but was {1}", sizes [i], grid.MeasuredElements [i].Value, message, i);
				}
			}
		}
		public static void CheckArrangeArgs (this MyGrid grid, string message, params Size [] sizes)
		{
			Assert.AreEqual (sizes.Length, grid.ArrangedElements.Count, "Wrong number of elements were arranged. {0}", message);
			for (int i = 0; i < grid.ArrangedElements.Count; i++) {
				try {
					Assert.IsBetween (sizes [i].Height - 0.55, sizes [i].Height + 0.55, grid.ArrangedElements [i].Value.Height, "#1");
					Assert.IsBetween (sizes [i].Width - 0.55, sizes [i].Width + 0.55, grid.ArrangedElements [i].Value.Width, "#1");
				} catch {
					Assert.Fail ("{2}.{3} Expected arrange argument to be {0} but was {1}", sizes [i], grid.ArrangedElements [i].Value, message, i);
				}
			}
		}

		//public static void CheckMeasureOrder (this MyGrid grid, string message, params UIElement [] elements)
		//{
		//    UIElement [] measured = grid.MeasuredElements.Select (d => d.Key).ToArray ();
		//    for (int i = 0; i < measured.Length; i++)
		//        Assert.AreSame (elements [i], measured [i], message + "." + i);

		//}

		// Every time an element in the grid is measured, it adds itself and its Size argument to the
		// MeasuredElements list. This way we can see which elements are measured multiple times and also
		// the order in which elements are measured. This helper method just checks that the order in which
		// the children were measured is correct. Note: You can have the same child multiple times.
		public static void CheckMeasureOrder (this MyGrid grid, string message, params int [] childIndexes)
		{
			UIElement [] measured = grid.MeasuredElements.Select (d => d.Key).ToArray ();
			for (int i = 0; i < childIndexes.Length; i++) {
				string error = string.Format ("Child at index {0} was measured instead of child at index {1}", grid.Children.IndexOf (measured [i]), childIndexes [i]);
				Assert.AreSame (grid.Children [childIndexes [i]], measured [i], message + "." + i + " : " + error);
			}
		}

		public static void CheckMeasureSizes (this Grid grid, string message, params Size [] sizes)
		{
			for (int i = 0; i < grid.Children.Count; i++) {
				var poker = (MyContentControl) grid.Children [i];
				var arg = poker.MeasureOverrideArg;
				if (!arg.Equals (sizes [i]))
					Assert.Fail ("{2}.{3} Expected measure argument to be {0} but was {1}", sizes [i], arg, message, i);
			}
		}

		public static void CheckMeasureResult (this Grid grid, string message, params Size [] sizes)
		{
			for (int i = 0; i < grid.Children.Count; i++) {
				var poker = (MyContentControl) grid.Children [i];
				var result = poker.MeasureOverrideResult;
				if (!result.Equals (sizes [i]))
					Assert.Fail ("{2}.{3} Expected measure result to be {0} but was {1}", sizes [i], result, message, i);
			}
		}
		public static void CheckArrangeResult (this MyGrid grid, string message, params Size [] sizes)
		{
			Assert.AreEqual (sizes.Length, grid.ArrangeResultElements.Count, "Wrong number of elements were arranged. {0}", message);
			for (int i = 0; i < grid.ArrangeResultElements.Count; i++) {
				try {
					Assert.IsBetween (sizes [i].Height - 0.55, sizes [i].Height + 0.55, grid.ArrangeResultElements [i].Value.Height, "#1");
					Assert.IsBetween (sizes [i].Width - 0.55, sizes [i].Width + 0.55, grid.ArrangeResultElements [i].Value.Width, "#1");
				} catch {
					Assert.Fail ("{2}.{3} Expected arrange result to be {0} but was {1}", sizes [i], grid.ArrangeResultElements [i].Value, message, i);
				}
			}
		}

		public static void InvalidateSubtree (this UIElement element)
		{
			element.InvalidateArrange ();
			element.InvalidateMeasure ();
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount (element); i++)
				((UIElement) VisualTreeHelper.GetChild (element, i)).InvalidateSubtree ();
		}
	}

	public partial class GridTest
	{
		static MyGrid CreateGridWithChildren ()
		{
			MyGrid grid = new MyGrid { Name = "GridUnderTest" };
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (2, GridUnitType.Star), new GridLength (3, GridUnitType.Star));
			grid.AddColumns (new GridLength (1, GridUnitType.Star), new GridLength (2, GridUnitType.Star), new GridLength (3, GridUnitType.Star));

			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					grid.AddChild (new MyContentControl { Content = new Rectangle { Fill = new SolidColorBrush (Colors.Red), MinWidth = 15, MinHeight = 15 } }, i, j, 1, 1);
			return grid;
		}
		static FrameworkElement CreateInfiniteChild ()
		{
			// Creates a child (ScrollViewer) which will consume as much space as is available to it
			// and does *not* have an explicit width/height set on it.
			return new MyContentControl {
				Content = new ScrollViewer {
					Content = new Rectangle {
						Width = 300,
						Height = 300,
						Fill = new RadialGradientBrush (Colors.Red, Colors.Blue)
					}
				}
			};
		}
		static readonly Size Infinity = new Size (double.PositiveInfinity, double.PositiveInfinity);

		#region When do we expand star rows

		class SettablePanel : Panel
		{
			public Size? ArrangeArg { get; set; }
			public Size? MeasureArg { get; set; }
			public Grid Grid { get; set; }

			protected override Size ArrangeOverride (Size finalSize)
			{
				if (ArrangeArg.HasValue)
					Grid.Arrange (new Rect (0, 0, ArrangeArg.Value.Width, ArrangeArg.Value.Height));
				else
					Grid.Arrange (new Rect (0, 0, Grid.DesiredSize.Width, Grid.DesiredSize.Height));
				return Grid.RenderSize;
			}

			protected override Size MeasureOverride (Size availableSize)
			{
				if (MeasureArg.HasValue)
					Grid.Measure (MeasureArg.Value);
				else
					Grid.Measure (availableSize);
				return Grid.DesiredSize;
			}
		}

		[TestMethod]
		public void MeasureStarRowsWithChild ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#4");
			Assert.AreEqual (inf, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#7");
			Assert.AreEqual (100, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void MeasureStarRowsWithChild_ExplicitSize ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid { Width = 75, Height = 75 };
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#4");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#7");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void MeasureStarRowsWithChild_NoSpan ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#4");
			Assert.AreEqual (inf, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#7");
			Assert.AreEqual (100, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void MeasureStarRowsWithChild_NoSpan_ExplicitSize ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid {
				Width = 75,
				Height = 75,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#4");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#7");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void MeasureStarRowsWithChild2 ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", inf, inf);

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 50, 50);
		}

		[TestMethod]
		public void MeasureStarRowsWithChild2_ExplicitSize ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid { Width = 75, Height = 75 };
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", 37.5, 37.5);

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 37.5, 37.5);
		}

		[TestMethod]
		public void MeasureStarRowsWithChild2_NoSpan ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", inf, inf);

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 50, 50);
		}

		[TestMethod]
		public void MeasureStarRowsWithChild2_NoSpan_ExplicitSize ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid {
				Width = 75,
				Height = 75,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", 37.5, 37.5);

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 37.5, 37.5);
		}


		[TestMethod]
		public void StarRowsWithChild ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#4");
			Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#7");
			Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void StarRowsWithChild_ExplicitSize ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid { Width = 75, Height = 75 };
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#4");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#7");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void StarRowsWithChild_NoSpan ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#4");
			Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#7");
			Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void StarRowsWithChild_NoSpan_ExplicitSize ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid {
				Width = 75,
				Height = 75,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#4");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#5");

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#7");
			Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#8");
		}

		[TestMethod]
		public void StarRowsWithChild2 ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", 50, 0);

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 50, 0);
		}

		[TestMethod]
		public void StarRowsWithChild2_ExplicitSize ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid { Width = 75, Height = 75 };
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", 37.5, 37.5);

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 37.5, 37.5);
		}

		[TestMethod]
		public void StarRowsWithChild2_NoSpan ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", 50, 0);

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 50, 0);
		}

		[TestMethod]
		public void StarRowsWithChild2_NoSpan_ExplicitSize ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid {
				Width = 75,
				Height = 75,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			grid.CheckRowHeights ("#2", 0, 0);

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
			grid.CheckRowHeights ("#4", 37.5, 37.5);

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#5");
			grid.CheckRowHeights ("#6", 37.5, 37.5);
		}


		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild_InTree ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid ();
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};

			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#1");
					Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#2");

					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
					Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#4");
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild_ExplicitSize_InTree ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid { Width = 75, Height = 75 };
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#1");
					Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#2");

					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
					Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#4");
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild_NoSpan_InTree ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {

					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#4");
					Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#5");
					poker.MeasureArg = new Size (100, 100);
				}, () => {

					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#7");
					Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#8");
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild_NoSpan_ExplicitSize_InTree ()
		{
			// Check what happens if there is no explicit ColumnDefinition added
			Grid grid = new Grid {
				Width = 75,
				Height = 75,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#1");
					Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#2");
					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
					Assert.AreEqual (75, grid.RowDefinitions [0].ActualHeight, "#4");
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild2_InTree ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid ();
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity,
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#1");
					grid.CheckRowHeights ("#2", 50, 0);
					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
					grid.CheckRowHeights ("#4", 50, 0);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild2_ExplicitSize_InTree ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid { Width = 75, Height = 75 };
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#1");
					grid.CheckRowHeights ("#2", 37.5, 37.5);
					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
					grid.CheckRowHeights ("#4", 37.5, 37.5);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild2_NoSpan_InTree ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#1");
					grid.CheckRowHeights ("#2", 50, 0);
					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
					grid.CheckRowHeights ("#4", 50, 0);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRowsWithChild2_NoSpan_ExplicitSize_InTree ()
		{
			// Check what happens when there are two explicit rows and no explicit column
			Grid grid = new Grid {
				Width = 75,
				Height = 75,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
			};
			var poker = new SettablePanel {
				Grid = grid,
				MeasureArg = Infinity
			};
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Star));
			grid.Children.Add (new MyContentControl (50, 50));

			CreateAsyncTest (poker,
				() => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#1");
					grid.CheckRowHeights ("#2", 37.5, 37.5);
					poker.MeasureArg = new Size (100, 100);
					poker.InvalidateSubtree ();
				}, () => {
					Assert.AreEqual (new Size (75, 75), grid.DesiredSize, "#3");
					grid.CheckRowHeights ("#4", 37.5, 37.5);
				}
			);
		}

		[TestMethod]
		public void ExpandInArrange_OutsideTree_NoParent_UnfixedSize ()
		{
			// We always expand star rows if we're not in the live tree
			// with no parent

			// Measure with infinity and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			grid.Measure (Infinity);
			grid.CheckMeasureArgs ("#1", Infinity);
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			grid.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#8"); // No remeasures
			grid.CheckArrangeArgs ("#9", new Size (100, 100));
			grid.CheckArrangeResult ("#10", new Size (100, 100));
			grid.CheckRowHeights ("#11", 100);
			grid.CheckColWidths ("#12", 100);

			// If we measure with a finite size, the rows/cols still expand
			// to consume the available space
			grid.Reset ();
			grid.Measure (new Size (1000, 1000));
			grid.CheckMeasureArgs ("#13", new Size (1000, 1000));
			grid.CheckMeasureResult ("#14", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#15");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#16", grid.DesiredSize);
			grid.CheckArrangeResult ("#17", grid.DesiredSize);
			grid.CheckRowHeights ("#18", grid.DesiredSize.Height);
			grid.CheckColWidths ("#19", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			grid.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#20"); // No remeasures
			grid.CheckArrangeArgs ("#21", new Size (100, 100));
			grid.CheckArrangeResult ("#22", new Size (100, 100));
			grid.CheckRowHeights ("#23", 100);
			grid.CheckColWidths ("#24", 100);
		}

		[TestMethod]
		public void ExpandInArrange_OutsideTree_GridParent_UnfixedSize ()
		{
			// We always expand star rows if we're not in the live tree
			// with a parent
			var parent = new Grid ();

			// Measure with infinity and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			parent.Children.Add (grid);

			parent.Measure (Infinity);
			grid.CheckMeasureArgs ("#1", Infinity);
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#8"); // No remeasures
			grid.CheckArrangeArgs ("#9", new Size (100, 100));
			grid.CheckArrangeResult ("#10", new Size (100, 100));
			grid.CheckRowHeights ("#11", 100);
			grid.CheckColWidths ("#12", 100);

			// If we measure with a finite size, the rows/cols still expand
			// to consume the available space
			grid.Reset ();
			parent.Measure (new Size (1000, 1000));
			grid.CheckMeasureArgs ("#13", new Size (1000, 1000));
			grid.CheckMeasureResult ("#14", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#15");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#16", grid.DesiredSize);
			grid.CheckArrangeResult ("#17", grid.DesiredSize);
			grid.CheckRowHeights ("#18", grid.DesiredSize.Height);
			grid.CheckColWidths ("#19", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#20"); // No remeasures
			grid.CheckArrangeArgs ("#21", new Size (100, 100));
			grid.CheckArrangeResult ("#22", new Size (100, 100));
			grid.CheckRowHeights ("#23", 100);
			grid.CheckColWidths ("#24", 100);
		}

		[TestMethod]
		public void ExpandInArrange_OutsideTree_BorderParent_UnfixedSize ()
		{
			// We always expand star rows if we're not in the live tree
			// with a parent
			var parent = new Border ();

			// Measure with infinity and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			parent.Child = grid;
			parent.InvalidateSubtree ();

			parent.Measure (Infinity);
			grid.CheckMeasureArgs ("#1", Infinity);
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#8"); // No remeasures
			grid.CheckArrangeArgs ("#9", new Size (100, 100));
			grid.CheckArrangeResult ("#10", new Size (100, 100));
			grid.CheckRowHeights ("#11", 100);
			grid.CheckColWidths ("#12", 100);

			// If we measure with a finite size, the rows/cols still expand
			// to consume the available space
			grid.Reset ();
			parent.InvalidateSubtree ();
			parent.Measure (new Size (1000, 1000));
			grid.CheckMeasureArgs ("#13", new Size (1000, 1000));
			grid.CheckMeasureResult ("#14", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#15");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#16", grid.DesiredSize);
			grid.CheckArrangeResult ("#17", grid.DesiredSize);
			grid.CheckRowHeights ("#18", grid.DesiredSize.Height);
			grid.CheckColWidths ("#19", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#20"); // No remeasures
			grid.CheckArrangeArgs ("#21", new Size (100, 100));
			grid.CheckArrangeResult ("#22", new Size (100, 100));
			grid.CheckRowHeights ("#23", 100);
			grid.CheckColWidths ("#24", 100);
		}

		[TestMethod]
		public void ExpandInArrange_OutsideTree_GridParent_FixedSize ()
		{
			// We always expand star rows if we're not in the live tree
			// with a parent
			var parent = new Grid ();

			// Measure with infinity and check results.
			MyGrid grid = new MyGrid { Width = 200, Height = 200 };
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			parent.Children.Add (grid);

			parent.Measure (Infinity);
			grid.CheckMeasureArgs ("#1", new Size (200, 200));
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (200, 200), grid.DesiredSize, "#3");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#8"); // No remeasures
			grid.CheckArrangeArgs ("#9"); // No rearranges
			grid.CheckArrangeResult ("#10");
			grid.CheckRowHeights ("#11", 200);
			grid.CheckColWidths ("#12", 200);

			// If we measure with a finite size, the rows/cols still expand
			// to consume the available space
			grid.Reset ();
			parent.Measure (new Size (150, 150));
			grid.CheckMeasureArgs ("#13"); // No remeasures
			grid.CheckMeasureResult ("#14", new Size (50, 50));
			Assert.AreEqual (new Size (150, 150), grid.DesiredSize, "#15");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#16");
			grid.CheckArrangeResult ("#17");
			grid.CheckRowHeights ("#18", 200);
			grid.CheckColWidths ("#19", 200);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#20"); // No remeasures
			grid.CheckArrangeArgs ("#21"); // No rearranges
			grid.CheckRowHeights ("#23", 200);
			grid.CheckColWidths ("#24", 200);
		}

		[TestMethod]
		public void ExpandInArrange_OutsideTree_BorderParent_FixedSize ()
		{
			// We always expand star rows if we're not in the live tree
			var parent = new Border ();

			// Measure with infinity and check results.
			MyGrid grid = new MyGrid { Width = 200, Height = 200 };
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			parent.Child = grid;
			parent.InvalidateSubtree ();

			parent.Measure (Infinity);
			grid.CheckMeasureArgs ("#1", new Size (200, 200));
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (200, 200), grid.DesiredSize, "#3");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, 1000, 10000));
			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			// If we pass in twice the desired size, the rows/cols consume that too
			grid.Reset ();
			parent.Arrange (new Rect (0, 0, 5, 5));
			grid.CheckMeasureArgs ("#8"); // No re-measuring
			grid.CheckArrangeArgs ("#9"); // No re-arranging
			grid.CheckArrangeResult ("#10");
			grid.CheckRowHeights ("#11", 200);
			grid.CheckColWidths ("#12", 200);

			// If we measure with a finite size, the rows/cols still expand
			// to consume the available space
			grid.Reset ();
			parent.InvalidateSubtree ();
			parent.Measure (new Size (1000, 1000));
			grid.CheckMeasureArgs ("#13", new Size (200, 200));
			grid.CheckMeasureResult ("#14", new Size (50, 50));
			Assert.AreEqual (new Size (200, 200), grid.DesiredSize, "#15");

			// When we pass in the desired size as the arrange arg,
			// the rows/cols use that as their height/width
			parent.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#16", grid.DesiredSize);
			grid.CheckArrangeResult ("#17", grid.DesiredSize);
			grid.CheckRowHeights ("#18", grid.DesiredSize.Height);
			grid.CheckColWidths ("#19", grid.DesiredSize.Width);
		}

		[TestMethod]
		[Asynchronous]
		public void ExpandInArrange_GridParent ()
		{
			// Measure with infinity and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.Reset ();
				TestPanel.Measure (Infinity);
				grid.CheckMeasureArgs ("#1", Infinity);
				grid.CheckMeasureResult ("#2", new Size (50, 50));
				Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");

				// When we pass in the desired size as the arrange arg,
				// the rows/cols use that as their height/width
				grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
				grid.CheckArrangeArgs ("#4", grid.DesiredSize);
				grid.CheckArrangeResult ("#5", grid.DesiredSize);
				grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
				grid.CheckColWidths ("#7", grid.DesiredSize.Width);

				// If we pass in twice the desired size, the rows/cols consume that too
				grid.Reset ();
				grid.Arrange (new Rect (0, 0, 100, 100));
				grid.CheckMeasureArgs ("#8"); // No remeasures
				grid.CheckArrangeArgs ("#9", new Size (100, 100));
				grid.CheckArrangeResult ("#10", new Size (100, 100));
				grid.CheckRowHeights ("#11", 100);
				grid.CheckColWidths ("#12", 100);

				// If we measure with a finite size, the rows/cols still expand
				// to consume the available space
				grid.Reset ();
				grid.Measure (new Size (1000, 1000));
				grid.CheckMeasureArgs ("#13", new Size (1000, 1000));
				grid.CheckMeasureResult ("#14", new Size (50, 50));
				Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#15");

				// When we pass in the desired size as the arrange arg,
				// the rows/cols use that as their height/width
				grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
				grid.CheckArrangeArgs ("#16", grid.DesiredSize);
				grid.CheckArrangeResult ("#17", grid.DesiredSize);
				grid.CheckRowHeights ("#18", grid.DesiredSize.Height);
				grid.CheckColWidths ("#19", grid.DesiredSize.Width);

				// If we pass in twice the desired size, the rows/cols consume that too
				grid.Reset ();
				grid.Arrange (new Rect (0, 0, 100, 100));
				grid.CheckMeasureArgs ("#20"); // No remeasures
				grid.CheckArrangeArgs ("#21", new Size (100, 100));
				grid.CheckArrangeResult ("#22", new Size (100, 100));
				grid.CheckRowHeights ("#23", 100);
				grid.CheckColWidths ("#24", 100);
			});
		}

		[TestMethod]
		[Asynchronous]
		public void ExpandInArrange_CanvasParent ()
		{
			// Measure with infinity and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			var parent = new Canvas ();
			parent.Children.Add (grid);
			CreateAsyncTest (parent, () => {
				grid.Reset ();
				TestPanel.Measure (Infinity);
				// Nothing is measured as the grid always uses (Inf, Inf) to measure children.
				grid.CheckMeasureArgs ("#1");
				Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");

				grid.Reset ();
				grid.Arrange (new Rect (0, 0, 100, 100));
				grid.CheckMeasureArgs ("#8"); // No remeasures
				grid.CheckArrangeArgs ("#9", new Size (100, 100));
				grid.CheckArrangeResult ("#10", new Size (100, 100));
				grid.CheckRowHeights ("#11", 100);
				grid.CheckColWidths ("#12", 100);

				// If we measure with a finite size, the rows/cols still expand
				// to consume the available space
				grid.Reset ();
				grid.Measure (new Size (1000, 1000));
				grid.CheckMeasureArgs ("#13", new Size (1000, 1000));
				grid.CheckMeasureResult ("#14", new Size (50, 50));
				Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#15");

				// When we pass in the desired size as the arrange arg,
				// the rows/cols use that as their height/width
				grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
				grid.CheckArrangeArgs ("#16", grid.DesiredSize);
				grid.CheckArrangeResult ("#17", grid.DesiredSize);
				grid.CheckRowHeights ("#18", grid.DesiredSize.Height);
				grid.CheckColWidths ("#19", grid.DesiredSize.Width);

				// If we pass in twice the desired size, the rows/cols consume that too
				grid.Reset ();
				grid.Arrange (new Rect (0, 0, 100, 100));
				grid.CheckMeasureArgs ("#20"); // No remeasures
				grid.CheckArrangeArgs ("#21", new Size (100, 100));
				grid.CheckArrangeResult ("#22", new Size (100, 100));
				grid.CheckRowHeights ("#23", 100);
				grid.CheckColWidths ("#24", 100);
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void ExpandStars_UnfixedSize ()
		{
			// If a width/height is *not* set on the grid, it doesn't expand stars.
			var canvas = new Canvas { Width = 120, Height = 120 };
			PanelPoker poker = new PanelPoker ();
			MyGrid grid = new MyGrid { Name = "TEDDY" };
			grid.AddRows (Star, Star, Star);
			grid.AddColumns (Star, Star, Star);

			canvas.Children.Add (poker);
			poker.Grid = grid;
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (canvas,
				() => {
					Assert.AreEqual (Infinity, poker.MeasureArgs [0], "#1");
					Assert.AreEqual (new Size (100, 100), poker.MeasureResults [0], "#2");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeArgs [0], "#3");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeResults [0], "#4");

					grid.CheckRowHeights ("#5", 0, 100, 0);
					grid.CheckColWidths ("#6", 0, 100, 0);

					grid.CheckMeasureArgs ("#7", Infinity);
					grid.CheckMeasureResult ("#8", new Size (100, 100));

					grid.CheckArrangeArgs ("#9", new Size (100, 100));
					grid.CheckArrangeResult ("#10", new Size (100, 100));

					// Do not expand if we already consume 100 px
					grid.Reset ();
					grid.Arrange (new Rect (0, 0, 100, 100));
					grid.CheckArrangeArgs ("#11");

					// If we give extra space, we expand the rows.
					grid.Arrange (new Rect (0, 0, 500, 500));

					grid.CheckRowHeights ("#12", 167, 167, 166);
					grid.CheckColWidths ("#13", 167, 167, 166);

					grid.CheckArrangeArgs ("#14", new Size (167, 167));
					grid.CheckArrangeResult ("#15", new Size (167, 167));
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void ExpandStars_FixedSize ()
		{
			// If a width/height is set on the grid, it expands stars.
			var canvas = new Canvas { Width = 120, Height = 120 };
			PanelPoker poker = new PanelPoker { Width = 120, Height = 120 };
			MyGrid grid = new MyGrid { Name = "Griddy" };
			grid.AddRows (Star, Star, Star);
			grid.AddColumns (Star, Star, Star);

			canvas.Children.Add (poker);
			poker.Grid = grid;
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (canvas,
				() => {
					Assert.AreEqual (new Size (120, 120), poker.MeasureArgs [0], "#1");
					Assert.AreEqual (new Size (40, 40), poker.MeasureResults [0], "#2");
					Assert.AreEqual (new Size (120, 120), poker.ArrangeArgs [0], "#3");
					Assert.AreEqual (new Size (120, 120), poker.ArrangeResults [0], "#4");

					grid.CheckRowHeights ("#5", 40, 40, 40);
					grid.CheckColWidths ("#6", 40, 40, 40);

					grid.CheckMeasureArgs ("#7", new Size (40, 40));
					grid.CheckMeasureResult ("#8", new Size (40, 40));

					grid.CheckArrangeArgs ("#9", new Size (40, 40));
					grid.CheckArrangeResult ("#10", new Size (40, 40));
				}
			);
		}

		[TestMethod]
		public void ExpandStars_NoRowsOrCols ()
		{
			// If the rows/cols are autogenerated, we still expand them
			Grid grid = new Grid ();
			grid.Children.Add (new Rectangle { Width = 50, Height = 50 });

			grid.Measure (new Size (200, 200));
			grid.Arrange (new Rect (0, 0, 200, 200));

			Assert.AreEqual (200, grid.ActualWidth, "#1");
			Assert.AreEqual (200, grid.ActualHeight, "#2");
		}

		[TestMethod]
		public void ExpandStars_NoRowsOrCols2 ()
		{
			// We don't expand autogenerated rows/cols if we don't have Alignment.Stretch
			Grid grid = new Grid { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
			grid.Children.Add (new Rectangle { Width = 50, Height = 50 });

			grid.Measure (new Size (200, 200));
			grid.Arrange (new Rect (0, 0, 200, 200));

			Assert.AreEqual (50, grid.ActualWidth, "#1");
			Assert.AreEqual (50, grid.ActualHeight, "#2");
		}

		[TestMethod]
		[Asynchronous]
		[MinRuntimeVersion (4)]
		public void ArrangeOverride_Constraints ()
		{
			MyContentControl top = new MyContentControl { Width = 100, Height = 100 };
			MyContentControl child = new MyContentControl ();
			Rectangle content = new Rectangle { Width = 50, Height = 50 };

			top.Content = child;
			child.Content = content;

			CreateAsyncTest (top, () => {
				// First check the natural results.
				Assert.AreEqual (new Size (100, 100), child.MeasureOverrideArg, "#1");
				Assert.AreEqual (new Size (50, 50), child.MeasureOverrideResult, "#2");
				Assert.AreEqual (new Size (50, 50), child.DesiredSize, "desired 1");

                // The moonlight test had the following checks below:
                //     Assert.AreEqual (new Size (50, 50), child.ArrangeOverrideArg, "#4");
                //     Assert.AreEqual (new Size (50, 50), child.ArrangeOverrideResult, "#5");
                //     Assert.AreEqual(new Size(50, 50), child.RenderSize, "#3");
                // But running this test in WPF gives us the same results as we get here, so
                // assuming that the test is wrong.
                Assert.AreEqual (new Size (100, 100), child.ArrangeOverrideArg, "#4");
				Assert.AreEqual (new Size (100, 100), child.ArrangeOverrideResult, "#5");
				Assert.AreEqual (new Size (100, 100), child.RenderSize, "#3");

				Assert.AreEqual (new Size (50, 50), content.DesiredSize, "desired 2");

				// Now give the child more size in Arrange than it requires.
				child.Arrange (new Rect (0, 0, 100, 100));
				Assert.AreEqual (new Size (100, 100), child.RenderSize, "#8");
				Assert.AreEqual (new Size (100, 100), child.ArrangeOverrideArg, "#9");
				Assert.AreEqual (new Size (100, 100), child.ArrangeOverrideResult, "#10");
				Assert.AreEqual (new Size (50, 50), content.RenderSize, "#16");

                // Now give the child less size
				child.Arrange (new Rect (0, 0, 10, 10));
				Assert.AreEqual (new Size (50, 50), child.RenderSize, "#13");
				Assert.AreEqual (new Size (50, 50), child.ArrangeOverrideArg, "#14");
				Assert.AreEqual (new Size (50, 50), child.ArrangeOverrideResult, "#15");

				Assert.AreEqual (new Size (50, 50), content.RenderSize, "#16");
			});
		}

		[TestMethod]
		[Asynchronous]
		public void ArrangeOverride_Constraints2 ()
		{
			MyContentControl top = new MyContentControl { Width = 25, Height = 25 };
			MyContentControl child = new MyContentControl ();
			Rectangle content = new Rectangle { Width = 50, Height = 50 };

			top.Content = child;
			child.Content = content;

			CreateAsyncTest (top, () => {
				// First check the natural results.
				Assert.AreEqual (new Size (25, 25), child.DesiredSize, "desired 1");
				Assert.AreEqual (new Size (25, 25), content.DesiredSize, "desired 2");

				Assert.AreEqual (new Size (25, 25), child.MeasureOverrideArg, "#1");
				Assert.AreEqual (new Size (25, 25), child.MeasureOverrideResult, "#2");
				Assert.AreEqual (new Size (25, 25), child.ArrangeOverrideArg, "#4");
				Assert.AreEqual (new Size (25, 25), child.ArrangeOverrideResult, "#5");
				Assert.AreEqual (new Size (25, 25), child.RenderSize, "#3");


				Assert.AreEqual (new Size (25, 25), new Size (child.ActualWidth, child.ActualHeight), "actual 1");
				Assert.AreEqual (new Size (50, 50), new Size (content.ActualWidth, content.ActualHeight), "actual 2");

				// Now give the child more size in Arrange than it requires.
				child.Arrange (new Rect (0, 0, 100, 100));
				Assert.AreEqual (new Size (25, 25), child.DesiredSize, "desired 3");
				Assert.AreEqual (new Size (25, 25), content.DesiredSize, "desired 4");
				Assert.AreEqual (new Size (100, 100), new Size (child.ActualWidth, child.ActualHeight), "actual 3");
				Assert.AreEqual (new Size (50, 50), new Size (content.ActualWidth, content.ActualHeight), "actual 4");

				Assert.AreEqual (new Size (100, 100), child.RenderSize, "#8");
				Assert.AreEqual (new Size (100, 100), child.ArrangeOverrideArg, "#9");
				Assert.AreEqual (new Size (100, 100), child.ArrangeOverrideResult, "#10");
				Assert.AreEqual (new Size (50, 50), content.RenderSize, "#11");

				// Now give the child less size
				child.Arrange (new Rect (0, 0, 10, 10));
				Assert.AreEqual (new Size (25, 25), child.DesiredSize, "desired 5");
				Assert.AreEqual (new Size (25, 25), content.DesiredSize, "desired 6");
				Assert.AreEqual (new Size (25, 25), new Size (child.ActualWidth, child.ActualHeight), "actual 5");
				Assert.AreEqual (new Size (50, 50), new Size (content.ActualWidth, content.ActualHeight), "actual 6");

				Assert.AreEqual (new Size (25, 25), child.RenderSize, "#13");
				Assert.AreEqual (new Size (25, 25), child.ArrangeOverrideArg, "#14");
				Assert.AreEqual (new Size (25, 25), child.ArrangeOverrideResult, "#15");
			});
		}

		[TestMethod]
		public void ExpandInArrange2 ()
		{
			// Measure with a finite value and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			grid.Measure (new Size (75, 75));
			grid.CheckMeasureArgs ("#1", new Size (75, 75));
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");

			// Check that everything is as expected when we pass in DesiredSize as the argument to Arrange
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			grid.Reset ();
			grid.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#8"); // No remeasures
			grid.CheckArrangeArgs ("#9", new Size (100, 100));
			grid.CheckArrangeResult ("#10", new Size (100, 100));
			grid.CheckRowHeights ("#11", 100);
			grid.CheckColWidths ("#12", 100);
		}
		
		[TestMethod]
		[Asynchronous]
		public void StarRows3b2 ()
		{
			var canvas = new Canvas { Width = 120, Height = 120 };
			PanelPoker poker = new PanelPoker ();
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star, Star, Star);
			grid.AddColumns (Star, Star, Star);

			canvas.Children.Add (poker);
			poker.Grid = grid;
			poker.Children.Add (grid);
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (canvas,
				() => { },
				() => {
					Assert.AreEqual (Infinity, poker.MeasureArgs [0], "#1");
					Assert.AreEqual (new Size (100, 100), poker.MeasureResults [0], "#2");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeArgs [0], "#3");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeResults [0], "#4");

					grid.CheckColWidths ("#5", 0, 100, 0);
					grid.CheckRowHeights ("#6", 0, 100, 0);

					grid.CheckMeasureArgs ("#7", Infinity);
					grid.CheckMeasureResult ("#8", new Size (100, 100));

					grid.CheckArrangeArgs ("#9", new Size (100, 100));
					grid.CheckArrangeResult ("#10", new Size (100, 100));
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRows3c ()
		{
			var canvas = new Canvas { Width = 120, Height = 120 };
			var poker = new MyContentControl ();
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star, Star, Star);
			grid.AddColumns (Star, Star, Star);

			canvas.Children.Add (poker);
			poker.Content = grid;
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (canvas,
				() => { },
				() => {
					Assert.AreEqual (Infinity, poker.MeasureOverrideArg, "#1");
					Assert.AreEqual (new Size (100, 100), poker.MeasureOverrideResult, "#2");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeOverrideArg, "#3");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeOverrideResult, "#4");

					grid.CheckColWidths ("#5", 0, 100, 0);
					grid.CheckRowHeights ("#6", 0, 100, 0);

					grid.CheckMeasureArgs ("#7", Infinity);
					grid.CheckMeasureResult ("#8", new Size (100, 100));

					grid.CheckArrangeArgs ("#9", new Size (100, 100));
					grid.CheckArrangeResult ("#10", new Size (100, 100));
				}
			);
		}
		
		[TestMethod]
		[Asynchronous]
		[MinRuntimeVersion (4)]
		public void StarRows3d ()
		{
			var poker = new MyContentControl { Width = 120, Height = 120 };
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star, Star, Star);
			grid.AddColumns (Star, Star, Star);

			poker.Content = grid;
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (poker,
				() => { },
				() => {
					Assert.AreEqual (new Size (120, 120), poker.MeasureOverrideArg, "#1");
					Assert.AreEqual (new Size (40, 40), poker.MeasureOverrideResult, "#2");
					Assert.AreEqual (new Size (40, 40), grid.DesiredSize, "#2b");
					Assert.AreEqual (new Size (120, 120), poker.DesiredSize, "#2c");
					Assert.AreEqual (new Size (120, 120), poker.ArrangeOverrideArg, "#3");
					Assert.AreEqual (new Size (120, 120), poker.ArrangeOverrideResult, "#4");

					grid.CheckColWidths ("#5", 0, 40, 0);
					grid.CheckRowHeights ("#6", 0, 40, 0);

					grid.CheckMeasureArgs ("#7", new Size (40, 40));
					grid.CheckMeasureResult ("#8", new Size (40, 40));

					grid.CheckArrangeArgs ("#9", new Size (40, 40));
					grid.CheckArrangeResult ("#10", new Size (40, 40));
				}
			);
		}

		#endregion When do we expand star rows

		[TestMethod]
		public void ExpandInArrange ()
		{
			// Measure with infinity and check results.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star);
			grid.AddColumns (Star);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			grid.Measure (Infinity);
			grid.CheckMeasureArgs ("#1", Infinity);
			grid.CheckMeasureResult ("#2", new Size (50, 50));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#3");
			
			// Check that everything is as expected when we pass in DesiredSize as the argument to Arrange
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			grid.CheckArrangeArgs ("#4", grid.DesiredSize);
			grid.CheckArrangeResult ("#5", grid.DesiredSize);
			grid.CheckRowHeights ("#6", grid.DesiredSize.Height);
			grid.CheckColWidths ("#7", grid.DesiredSize.Width);

			grid.Reset ();
			grid.Arrange (new Rect (0, 0, 100, 100));
			grid.CheckMeasureArgs ("#8"); // No remeasures
			grid.CheckArrangeArgs ("#9", new Size (100, 100));
			grid.CheckArrangeResult ("#10", new Size (100, 100));
			grid.CheckRowHeights ("#11", 100);
			grid.CheckColWidths ("#12", 100);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoStarInfiniteChildren ()
		{
			Grid holder = new Grid { Width = 500, Height = 500 };
			MyGrid g = new MyGrid { Name = "Ted!" };
			g.AddRows (new GridLength (1, GridUnitType.Star), GridLength.Auto);
			g.AddColumns (new GridLength (1, GridUnitType.Star), GridLength.Auto);

			g.AddChild (CreateInfiniteChild (), 0, 0, 1, 1);
			g.AddChild (CreateInfiniteChild (), 0, 1, 1, 1);
			g.AddChild (CreateInfiniteChild (), 1, 0, 1, 1);
			g.AddChild (CreateInfiniteChild (), 1, 1, 1, 1);

			// FIXME: I think this fails because the first time the ScrollViewer measures it calculates
			// the visibility of the Horizontal/Vertical scroll bar incorrectly. It's desired size on the
			// first measure is (327, 327) whereas it should be (327, 310). A few measure cycles later and
			// it will be correct, but chews up much more CPU than it should.
			holder.Children.Add (g);
			CreateAsyncTest (holder, () => {
				g.CheckMeasureOrder ("#1", 3, 1, 2, 1, 0);
				g.CheckMeasureArgs ("#2", Infinity, Infinity, new Size (173, inf), new Size (inf, 190), new Size (173, 190));
				g.CheckMeasureResult ("#3", new Size (173, 190), new Size (327, 190), new Size (173, 310), new Size (327, 310), new Size (173, 310));
				g.CheckRowHeights ("#4", 190, 310);
				g.CheckColWidths ("#5", 173, 327);
				Assert.AreEqual (new Size (500, 500), g.DesiredSize, "#5");
			});
		}

		[TestMethod]
		[MoonlightBug ("ScrollViewerTest.ThumbResizes shows the same issue")]
		public void ChildInvalidatesGrid ()
		{
			var child = new MyContentControl (50, 50);
			Grid grid = new Grid ();
			grid.Children.Add (child);
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (50, 50), grid.DesiredSize, "#1");

			((FrameworkElement) child.Content).Height = 60;
			((FrameworkElement) child.Content).Width = 10;

			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (10, 60), grid.DesiredSize, "#2");
		}
		
		[TestMethod]
		[MoonlightBug ("ScrollViewerTest.ThumbResizes shows the same issue")]
		public void ChildInvalidatesGrid2 ()
		{
			var child = new MyContentControl (50, 50);
			MyGrid grid = new MyGrid ();
			grid.Children.Add (child);

			grid.Measure (new Size (100, 100));
			Assert.AreEqual (1, grid.MeasuredElements.Count, "#1");

			child.InvalidateMeasure ();
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (2, grid.MeasuredElements.Count, "#2");
		}

		[TestMethod]
		public void ChildInvalidatesGrid3 ()
		{
			var child = new MyContentControl (50, 50);
			MyGrid grid = new MyGrid ();
			grid.Children.Add (child);

			grid.Measure (new Size (100, 100));
			Assert.AreEqual (1, grid.MeasuredElements.Count, "#1");

			// Note that invalidating the measure of the content does
			// not invalidate the grid.
			((FrameworkElement) child.Content).InvalidateMeasure ();
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (1, grid.MeasuredElements.Count, "#2");
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void ExpandStarsInBorder ()
		{
			MyGrid grid = CreateGridWithChildren ();
			
			var parent = new Border ();
			parent.Child = grid;

			TestPanel.Width = 75;
			TestPanel.Height = 75;

			CreateAsyncTest (parent,
				() => {
					grid.CheckRowHeights ("#1", 12, 25, 38);

					grid.HorizontalAlignment = HorizontalAlignment.Left;
					grid.VerticalAlignment = VerticalAlignment.Center;
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#2", 12, 15, 15);
					grid.Width = 50;
					grid.Height = 50;
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#3", 8, 17, 25);

					grid.ClearValue (Grid.HorizontalAlignmentProperty);
					grid.ClearValue (Grid.VerticalAlignmentProperty);
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#4", 8, 17, 25);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void ExpandStarsInCanvas ()
		{
			Grid grid = CreateGridWithChildren ();

			var parent = new Canvas ();
			parent.Children.Add (grid);

			TestPanel.Width = 75;
			TestPanel.Height = 75;

			CreateAsyncTest (parent,
				() => {
					grid.CheckRowHeights ("#1", 15, 15, 15);

					grid.HorizontalAlignment = HorizontalAlignment.Left;
					grid.VerticalAlignment = VerticalAlignment.Center;
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#2", 15, 15, 15);

					grid.Width = 50;
					grid.Height = 50;
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#3", 8, 17, 25);

					grid.ClearValue (Grid.HorizontalAlignmentProperty);
					grid.ClearValue (Grid.VerticalAlignmentProperty);
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#4", 8, 17, 25);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void ExpandStarsInGrid ()
		{
			MyGrid grid = CreateGridWithChildren ();

			var parent = new Grid ();
			parent.AddRows (new GridLength (75));
			parent.AddColumns (new GridLength (75));
			parent.AddChild (grid, 0, 0, 1, 1);

			TestPanel.Width = 75;
			TestPanel.Height = 75;

			CreateAsyncTest (parent,
				() => {
					grid.CheckMeasureArgs ("#1a", new Size (12, 12), new Size (25, 12), new Size (38, 12),
												  new Size (12, 25), new Size (25, 25), new Size (38, 25),
												  new Size (12, 38), new Size (25, 38), new Size (38, 38));
					grid.CheckRowHeights ("#1", 12, 25, 38);

					grid.HorizontalAlignment = HorizontalAlignment.Left;
					grid.VerticalAlignment = VerticalAlignment.Center;
					parent.InvalidateSubtree ();
					grid.Reset ();
				}, () => {
					grid.CheckMeasureArgs ("#2a", new Size (12, 12), new Size (25, 12), new Size (38, 12),
												  new Size (12, 25), new Size (25, 25), new Size (38, 25),
												  new Size (12, 38), new Size (25, 38), new Size (38, 38));
					grid.CheckRowHeights ("#2", 12, 15, 15);

					grid.Width = 50;
					grid.Height = 50;
					parent.InvalidateSubtree ();
					grid.Reset ();
				}, () => {
					grid.CheckMeasureArgs ("#3a", new Size (8, 8), new Size (17, 8), new Size (25, 8),
												  new Size (8, 17), new Size (17, 17), new Size (25, 17),
												  new Size (8, 25), new Size (17, 25), new Size (25, 25));
					grid.CheckRowHeights ("#3", 8, 17, 25);

					grid.ClearValue (Grid.HorizontalAlignmentProperty);
					grid.ClearValue (Grid.VerticalAlignmentProperty);
					parent.InvalidateSubtree ();
					grid.Reset ();
				}, () => {
					grid.CheckMeasureArgs ("#4a", new Size (8, 8), new Size (17, 8), new Size (25, 8),
												  new Size (8, 17), new Size (17, 17), new Size (25, 17),
												  new Size (8, 25), new Size (17, 25), new Size (25, 25));
					grid.CheckRowHeights ("#4", 8, 17, 25);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void ExpandStarsInStackPanel ()
		{
			MyGrid grid = CreateGridWithChildren ();
			var parent = new StackPanel ();
			parent.Children.Add (grid);

			TestPanel.Width = 75;
			TestPanel.Height = 75;

			CreateAsyncTest (parent,
				() => {
					grid.CheckRowHeights ("#1", 15, 15, 15);
					grid.CheckColWidths ("#2", 12, 25, 38);
					
					grid.HorizontalAlignment = HorizontalAlignment.Left;
					grid.VerticalAlignment = VerticalAlignment.Center;
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#3", 15, 15, 15);
					grid.CheckColWidths ("#4", 12, 15, 15);

					grid.Width = 50;
					grid.Height = 50;
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#5", 8, 17, 25);
					grid.CheckColWidths ("#6", 8, 17, 25);

					grid.ClearValue (Grid.HorizontalAlignmentProperty);
					grid.ClearValue (Grid.VerticalAlignmentProperty);
					parent.InvalidateSubtree ();
				}, () => {
					grid.CheckRowHeights ("#7", 8, 17, 25);
					grid.CheckColWidths ("#8", 8, 17, 25);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void ExpandStarsInStackPanel2 ()
		{
			Grid grid = new Grid ();
			grid.AddRows (Auto);
			grid.AddColumns (Auto);

			var parent = new StackPanel ();

			for (int i = 0; i < 4; i++) {
				MyGrid g = new MyGrid { Name = "Grid" + i };
				g.AddRows (Star);
				g.AddColumns (Star);
				g.Children.Add (new MyContentControl {
					Content = new Rectangle {
						RadiusX = 4,
						RadiusY = 4,
						StrokeThickness = 2,
						Fill = new SolidColorBrush (Colors.Red),
						Stroke = new SolidColorBrush (Colors.Black)
					}
				});
				g.Children.Add (new MyContentControl {
					Content = new Rectangle {
						Fill = new SolidColorBrush (Colors.Blue),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						Height = 17,
						Width = 20 + i * 20
					}
				});
				parent.Children.Add (g);
			}
			grid.Children.Add (parent);

			CreateAsyncTest (grid, () => {
				for (int i = 0 ;i < parent.Children.Count; i++) {
					MyGrid g = (MyGrid)parent.Children[i];
					Assert.AreEqual (new Size (20 + i * 20, 17), g.DesiredSize, "#1." + i);
					Assert.AreEqual (new Size (80, 17), g.RenderSize, "#2." + i);

					g.CheckMeasureArgs ("#3", Infinity, Infinity);
					g.CheckMeasureResult ("#4", new Size (0, 0), new Size (20 + i * 20, 17));

					g.CheckRowHeights ("#5", 17);
					g.CheckColWidths ("#6", 80);

					g.CheckArrangeArgs ("#7", new Size (80, 17), new Size (80, 17));
					g.CheckArrangeResult ("#8", new Size (80, 17), new Size (80, 17));
				}
			});
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureMaxAndMin ()
		{
			MyGrid g = new MyGrid ();
			var child = new MyContentControl (50, 50);
			g.AddColumns (GridLength.Auto);
			g.AddRows (GridLength.Auto, GridLength.Auto);
			g.AddChild (child, 0, 0, 1, 1);

			CreateAsyncTest (g,
				() => {
					g.CheckMeasureArgs ("#1", Infinity);
					g.CheckRowHeights ("#2", 50, 0);

					g.Reset ();
					g.InvalidateSubtree ();
					g.RowDefinitions [0].MaxHeight = 20;
				}, () => {
					g.CheckMeasureArgs ("#3", Infinity);
					g.CheckRowHeights ("#4", 50, 0);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureMaxAndMin2 ()
		{
			MyGrid g = new MyGrid ();
			var child = new MyContentControl (50, 50);
			g.AddColumns (new GridLength (50));
			g.AddRows (new GridLength (50), new GridLength (50));
			g.AddChild (child, 0, 0, 1, 1);

			CreateAsyncTest (g,
				() => {
					g.CheckMeasureArgs ("#1", new Size (50, 50));
					g.CheckRowHeights ("#2", 50, 50);

					g.Reset ();
					g.InvalidateSubtree ();
					g.RowDefinitions [0].MaxHeight = 20;
				}, () => {
					g.CheckMeasureArgs ("#3", new Size (50, 20));
					g.CheckRowHeights ("#4", 20, 50);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureMaxAndMin3 ()
		{
			Grid g = new Grid ();
			var child = new MyContentControl (50, 50);
			g.AddColumns (new GridLength (50));
			g.AddRows (new GridLength (20), new GridLength (20));
			g.AddChild (child, 0, 0, 2, 2);

			g.RowDefinitions [0].MaxHeight = 5;
			g.RowDefinitions [1].MaxHeight = 30;

			CreateAsyncTest (g,
				() => {
					var arg = child.MeasureOverrideArg;
					Assert.AreEqual (25, arg.Height, "#1");
					g.RowDefinitions [0].MaxHeight = 10;
				}, () => {
					var arg = child.MeasureOverrideArg;
					Assert.AreEqual (30, arg.Height, "#2");
					g.RowDefinitions [0].MaxHeight = 20;
				}, () => {
					var arg = child.MeasureOverrideArg;
					Assert.AreEqual (40, arg.Height, "#3");
				}
			);
		}

		[TestMethod]
		public void MeasureAutoRows ()
		{
			MyGrid grid = new MyGrid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);

			grid.AddChild (new MyContentControl (50, 50), 0, 0, 2, 1);
			grid.AddChild (new MyContentControl (50, 60), 0, 1, 1, 1);

			grid.Measure (new Size (0, 0));
			grid.CheckMeasureArgs ("#1", new Size (50, inf), new Size (50, inf));
			grid.Reset ();
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#2");

			grid.Measure (new Size (50, 40));
			grid.CheckMeasureSizes ("#3", new Size (50, inf), new Size (50, inf));
			grid.Reset ();
			Assert.AreEqual (new Size (50, 40), grid.DesiredSize, "#4");

			grid.Measure (new Size (500, 400));
			grid.CheckMeasureSizes ("#5", new Size (50, inf), new Size (50, inf));
			grid.Reset ();
			Assert.AreEqual (new Size (100, 60), grid.DesiredSize, "#6");
		}

		[TestMethod]
		public void MeasureAutoRows2 ()
		{
			double inf = double.PositiveInfinity;
			MyGrid grid = new MyGrid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);

			MyContentControl c = new MyContentControl (50, 50);
			grid.AddChild (c, 0, 0, 2, 1);
			grid.AddChild (new MyContentControl (50, 60), 0, 1, 1, 1);
			grid.AddChild (new MyContentControl (50, 20), 0, 1, 1, 1);

			grid.Measure (new Size (500, 400));
			grid.CheckMeasureArgs ("#1", new Size (50, inf), new Size (50, inf), new Size (50, inf));
			grid.CheckMeasureOrder ("#2", 0, 1, 2);
			Assert.AreEqual (new Size (100, 60), grid.DesiredSize, "#2");

			grid.ChangeRow (2, 1);
			grid.Reset ();
			grid.Measure (new Size (500, 400));
			grid.CheckMeasureArgs ("#3", new Size (50, inf));
			grid.CheckMeasureOrder ("#4", 2);
			Assert.AreEqual (new Size (100, 80), grid.DesiredSize, "#4");

			grid.InvalidateSubtree ();
			((FrameworkElement) c.Content).Height = 100;

			grid.Reset ();
			grid.Measure (new Size (500, 400));
			grid.CheckMeasureArgs ("#5", new Size (50, inf), new Size (50, inf), new Size (50, inf));
			Assert.AreEqual (new Size (100, 100), grid.DesiredSize, "#6");

			grid.Reset ();
			grid.ChangeRow (2, 2);
			grid.Measure (new Size (500, 400));
			grid.CheckMeasureArgs ("#7", new Size (50, inf));
			grid.CheckMeasureOrder ("#8", 2);
			Assert.AreEqual (new Size (100, 120), grid.DesiredSize, "#8");
		}

		[TestMethod]
		public void ChangingGridPropertiesInvalidates ()
		{
			// Normally remeasuring with the same width/height does not result in MeasureOverride
			// being called, but if we change a grid property, it does.
			MyGrid g = new MyGrid ();
			g.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);
			g.AddColumns (GridLength.Auto, GridLength.Auto, GridLength.Auto);
			g.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			g.Measure (new Size (50, 50));
			g.CheckMeasureArgs ("#1", new Size (inf, inf));

			g.Reset ();
			g.Measure (new Size (50, 50));
			g.CheckMeasureArgs ("#2");

			g.ChangeRowSpan (0, 2);
			g.Reset ();
			g.Measure (new Size (50, 50));
			g.CheckMeasureArgs ("#3", new Size (inf, inf));

			g.ChangeColSpan (0, 2);
			g.Reset ();
			g.Measure (new Size (50, 50));
			g.CheckMeasureArgs ("#4", new Size (inf, inf));

			g.ChangeRow (0, 1);
			g.Reset ();
			g.Measure (new Size (50, 50));
			g.CheckMeasureArgs ("#5", new Size (inf, inf));

			g.ChangeCol (0, 1);
			g.Reset ();
			g.Measure (new Size (50, 50));
			g.CheckMeasureArgs ("#6", new Size (inf, inf));
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureAutoRows3 ()
		{
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);

			grid.AddChild (new MyContentControl (50, 50), 0, 1, 2, 1);
			grid.AddChild (new MyContentControl (50, 60), 1, 1, 1, 1);
			grid.AddChild (new MyContentControl (50, 70), 0, 1, 3, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckRowHeights ("#1", 3.33, 63.33, 3.33);
			});
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureAutoRows4 ()
		{
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);

			grid.AddChild (new MyContentControl (50, 30), 0, 1, 3, 1);
			grid.AddChild (new MyContentControl (50, 90), 0, 1, 1, 1);
			grid.AddChild (new MyContentControl (50, 50), 0, 1, 2, 1);

			grid.AddChild (new MyContentControl (50, 70), 1, 1, 4, 1);
			grid.AddChild (new MyContentControl (50, 120), 1, 1, 2, 1);
			grid.AddChild (new MyContentControl (50, 30), 2, 1, 3, 1);

			grid.AddChild (new MyContentControl (50, 10), 3, 1, 1, 1);
			grid.AddChild (new MyContentControl (50, 50), 3, 1, 2, 1);
			grid.AddChild (new MyContentControl (50, 80), 3, 1, 2, 1);

			grid.AddChild (new MyContentControl (50, 20), 4, 1, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckRowHeights ("#1", 90, 60, 60, 35, 45);
			});
		}

		[TestMethod]
		public void MeasureAutoAndFixedRows ()
		{
			Grid grid = new Grid { };

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (new GridLength (20), new GridLength (20));
			grid.AddChild (new MyContentControl (50, 50), 0, 1, 2, 1);

			grid.Measure (Infinity);
			grid.CheckRowHeights ("#1", 20, 20);
			grid.CheckMeasureSizes ("#2", new Size (50, 40));
			Assert.AreEqual (new Size (100, 40), grid.DesiredSize, "#3");

			grid.RowDefinitions [0].Height = new GridLength (30);
			grid.Measure (Infinity);
			grid.CheckRowHeights ("#4", 30, 20);
			grid.CheckMeasureSizes ("#5", new Size (50, 50));
			Assert.AreEqual (new Size (100, 50), grid.DesiredSize, "#6");

			grid.RowDefinitions.Insert (0, new RowDefinition { Height = GridLength.Auto });
			grid.Measure (Infinity);
			grid.CheckRowHeights ("#7", double.PositiveInfinity, 30, 20);
			grid.CheckMeasureSizes ("#8", new Size (50, double.PositiveInfinity));
			Assert.AreEqual (new Size (100, 70), grid.DesiredSize, "#9");

			grid.Children.Clear ();
			grid.AddChild (new MyContentControl (50, 150), 0, 1, 2, 1);
			grid.Measure (Infinity);
			grid.CheckDesired ("#13", new Size (50, 150));
			grid.CheckRowHeights ("#10", double.PositiveInfinity, 30, 20);
			grid.CheckMeasureSizes ("#11", new Size (50, double.PositiveInfinity));
			grid.CheckMeasureResult ("#12", new Size (50, 150));
			Assert.AreEqual (new Size (100, 170), grid.DesiredSize, "#12");
		}

		[TestMethod]
		[MoonlightBug ("Layout rounding regression")]
		public void MeasureAutoAndStarRows ()
		{
			MyGrid grid = new MyGrid ();

			grid.AddColumns (new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, new GridLength (1, GridUnitType.Star), GridLength.Auto, GridLength.Auto);

			grid.AddChild (new MyContentControl (50, 50), 0, 0, 3, 1);
			grid.AddChild (new MyContentControl (50, 60), 1, 0, 3, 1);

			grid.Measure (new Size (100, 100));
			grid.CheckRowHeights ("#1", inf, inf, 100, inf, inf);
			grid.CheckMeasureArgs ("#2", new Size (50, 100), new Size (50, 100));
			grid.CheckMeasureOrder ("#3", 0, 1);
			Assert.AreEqual (new Size (50, 60), grid.DesiredSize, "#4");

			grid.RowDefinitions [2].MaxHeight = 15;
			grid.Reset ();
			grid.Measure (new Size (100, 100));
			grid.CheckRowHeights ("#5", inf, inf, 15, inf, inf);
			grid.CheckMeasureArgs ("#6", new Size (50, 15), new Size (50, 15));
			Assert.AreEqual (new Size (50, 15), grid.DesiredSize, "#7");

			grid.RowDefinitions.Clear ();
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength (1, GridUnitType.Star), GridLength.Auto);
			grid.Reset ();
			grid.Measure (new Size (100, 100));
			grid.CheckRowHeights ("#8", inf, inf, inf, 50, inf);
			grid.CheckMeasureArgs ("#9", new Size (50, inf), new Size (50, 83.33));
			Assert.AreEqual (new Size (50, 77), grid.DesiredSize, "#10");

			grid.RowDefinitions [3].MaxHeight = 15;
			grid.Reset ();
			grid.Measure (new Size (100, 100));
			grid.CheckRowHeights ("#11", inf, inf, inf, 15, inf);
			grid.CheckMeasureArgs ("#12", new Size (50, 48.8));
			grid.CheckMeasureOrder ("#13", 1);
			Assert.AreEqual (new Size (50, 65), grid.DesiredSize, "#12");
		}

		[TestMethod]
		public void StarStarRows_LimitedHeight_RowSpan_ExactSpace()
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = Star });
			grid.RowDefinitions.Add(new RowDefinition { MaxHeight = 20, Height = Star });

			var child1 = ContentControlWithChild();
			var child2 = ContentControlWithChild();
			(child1.Content as FrameworkElement).Height = 50;
			(child2.Content as FrameworkElement).Height = 70;

			grid.AddChild(child1, 0, 0, 1, 1);
			grid.AddChild(child2, 0, 0, 2, 1);

			Action<Size> sized = delegate {
				Assert.AreEqual(50, grid.RowDefinitions[0].ActualHeight, "#row 0 sized");
				Assert.AreEqual(20, grid.RowDefinitions[1].ActualHeight, "#row 1 sized");
			};

			child1.MeasureHook = sized;
			child2.MeasureHook = sized;
			grid.Measure(new Size(70, 70));

			// The row definitions have already been fully sized before the first
			// call to measure a child
			Assert.AreEqual(new Size(70, 50), child1.MeasureOverrideArg, "#1");
			Assert.AreEqual(new Size(70, 70), child2.MeasureOverrideArg, "#2");
			Assert.AreEqual(new Size(50, 70), grid.DesiredSize, "#3");
		}

		[TestMethod]
		public void StarStarRows_LimitedHeight_RowSpan_InfiniteSpace()
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = Star });
			grid.RowDefinitions.Add(new RowDefinition { MaxHeight = 20, Height = Star });

			var child1 = ContentControlWithChild();
			var child2 = ContentControlWithChild();
			(child1.Content as FrameworkElement).Height = 50;
			(child2.Content as FrameworkElement).Height = 70;
			grid.AddChild(child1, 0, 0, 1, 1);
			grid.AddChild(child2, 0, 0, 2, 1);

			grid.Measure(Infinity);
			Assert.AreEqual(Infinity, child1.MeasureOverrideArg, "#1");
			Assert.AreEqual(Infinity, child2.MeasureOverrideArg, "#2");
			Assert.AreEqual(new Size (50, 70), grid.DesiredSize, "#3");
		}

		[TestMethod]
		public void StarStarRows_StarCol_LimitedHeight()
		{
			var g = new Grid();
			var child = ContentControlWithChild();

			g.RowDefinitions.Add(new RowDefinition { Height = Star });
			g.RowDefinitions.Add(new RowDefinition { Height = Star, MaxHeight = 20 });
			g.AddChild(child, 0, 0, 1, 1);

			g.Measure(new Size(100, 100));
			Assert.AreEqual(new Size(100, 80), child.MeasureOverrideArg, "#1");
		}

		[TestMethod]
		public void StarRow_AutoCol_LimitedHeigth()
		{
			var g = new Grid();
			var child = ContentControlWithChild();

			g.RowDefinitions.Add(new RowDefinition { Height = Star });
			g.RowDefinitions.Add(new RowDefinition { Height = Star, MaxHeight = 20 });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = Auto });
			g.AddChild(child, 0, 0, 1, 1);

			g.Measure(new Size(100, 100));
			Assert.AreEqual(new Size(inf, 80), child.MeasureOverrideArg, "#1");
		}

		[TestMethod]
		public void StarRow_AutoStarCol_LimitedWidth()
		{
			var g = new Grid();
			var child = ContentControlWithChild();

			g.RowDefinitions.Add(new RowDefinition { Height = Star });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = Auto });
			g.ColumnDefinitions.Add(new ColumnDefinition { Width = Star, MaxWidth = 20 });
			g.AddChild(child, 0, 0, 1, 1);

			g.Measure(new Size(100, 100));
			Assert.AreEqual(new Size(inf, 100), child.MeasureOverrideArg, "#1");
		}

		[TestMethod]
		public void AutoRow_StarCol()
		{
			var g = new Grid();
			var child = ContentControlWithChild ();
			g.RowDefinitions.Add(new RowDefinition { Height = Star });
			g.RowDefinitions.Add(new RowDefinition { Height = Star, MaxHeight = 20 });

			g.AddChild(child, 0, 0, 1, 1);
			g.Measure(new Size(100, 100));
			Assert.AreEqual(new Size(100, 80), child.MeasureOverrideArg, "#1");
		}

		[TestMethod]
		[Asynchronous]
		public void FixedGridAllStar ()
		{
			// Specify the width/height on the grid and measure the widths/heights of the rows/cols
			GridLength oneStar = new GridLength (1, GridUnitType.Star);
			GridLength twoStar = new GridLength (2, GridUnitType.Star);
			GridLength threeStar = new GridLength (3, GridUnitType.Star);

			MyGrid g = new MyGrid { Name="Ted", ShowGridLines = true, Width = 240, Height = 240 };
			g.AddColumns (twoStar, oneStar, twoStar, oneStar);
			g.AddRows (oneStar, threeStar, oneStar, oneStar);
			CreateAsyncTest (g, () => {
				g.CheckRowHeights ("#1", 40, 120, 40, 40);
				g.CheckColWidths ("#2", 80, 40, 80, 40);
				Assert.AreEqual (new Size (240, 240), g.DesiredSize, "#3");
			});
		}

		[TestMethod]
		public void UnfixedGridAllStar ()
		{
			// Check the widths/heights of the rows/cols without specifying a size for the grid
			// Measuring the rows initialises the sizes to Infinity for 'star' elements
			Grid grid = new Grid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.AddColumns (new GridLength (1, GridUnitType.Star));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");
			Assert.AreEqual (0, grid.ColumnDefinitions [0].ActualWidth, "#3");

			// After measure
			grid.Measure (Infinity);
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#4");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#5");
			Assert.AreEqual (0, grid.ColumnDefinitions [0].ActualWidth, "#6");

			// Measure again
			grid.Measure (new Size (100, 100));
			grid.Arrange (new Rect (0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#7");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#8");
			Assert.AreEqual (0, grid.ColumnDefinitions [0].ActualWidth, "#9");
		}
		
		[TestMethod]
		public void MeasureStarRowsNoChild ()
		{
			// Measuring the rows initialises the sizes to Infinity for 'star' elements
			double inf = double.PositiveInfinity;
			Grid grid = new Grid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star));
			grid.AddColumns (new GridLength (1, GridUnitType.Star));

			// Initial values
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#1");
			Assert.AreEqual (0, grid.RowDefinitions [0].ActualHeight, "#2");
			Assert.AreEqual (0, grid.ColumnDefinitions [0].ActualWidth, "#3");

			// After measure
			grid.Measure (Infinity);
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#4");
			Assert.AreEqual (inf, grid.RowDefinitions [0].ActualHeight, "#5");
			Assert.AreEqual (inf, grid.ColumnDefinitions [0].ActualWidth, "#6");

			// Measure again
			grid.Measure (new Size (100, 100));
			Assert.AreEqual (new Size (0, 0), grid.DesiredSize, "#7");
			Assert.AreEqual (inf, grid.RowDefinitions [0].ActualHeight, "#8");
			Assert.AreEqual (inf, grid.ColumnDefinitions [0].ActualWidth, "#9");
		}

		[TestMethod]
		[Asynchronous]
		public void RowspanAutoTest ()
		{
			// This test demonstrates the following rules:
			// 1) Elements with RowSpan/ColSpan == 1 distribute their height first
			// 2) The rest of the elements distribute height in LIFO order
			Grid grid = new Grid ();
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);
			grid.AddColumns (new GridLength (50));

			var child50 = new MyContentControl (50, 50);
			var child60 = new MyContentControl (50, 60);

			grid.AddChild (child50, 0, 0, 1, 1);
			grid.AddChild (child60, 0, 0, 1, 1);

			CreateAsyncTest (grid,
				() => {
					// Check the initial values
					grid.CheckRowHeights ("#1", 60, 0, 0);

					// Now make the smaller element use rowspan = 2
					Grid.SetRowSpan (child50, 2);
				}, () => {
					grid.CheckRowHeights ("#2", 60, 0, 0);

					// Then make the larger element us rowspan = 2
					Grid.SetRowSpan (child50, 1);
					Grid.SetRowSpan (child60, 2);
				}, () => {
					grid.CheckRowHeights ("#3", 55, 5, 0);

					// Swap the order in which they are added to the grid
					grid.Children.Clear ();
					grid.AddChild (child60, 0, 0, 2, 0);
					grid.AddChild (child50, 0, 0, 1, 0);
				}, () => {
					// Swapping the order has no effect here
					grid.CheckRowHeights ("#4", 55, 5, 0);

					// Then give both rowspan = 2
					Grid.SetRowSpan (child50, 2);
				}, () => {
					grid.CheckRowHeights ("#5", 30, 30, 0);

					// Finally give the larger element rowspan = 3
					Grid.SetRowSpan (child60, 3);
				}, () => {
					grid.CheckRowHeights ("#6", 28.333, 28.333, 3.333);

					// Swap the order in which the elements are added again
					grid.Children.Clear ();
					grid.AddChild (child50, 0, 0, 2, 0);
					grid.AddChild (child60, 0, 0, 3, 0);
				}, () => {
					grid.CheckRowHeights ("#7", 25, 25, 20);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void SizeExceedsBounds ()
		{
			Grid grid = new Grid ();
			grid.RowDefinitions.Add (new RowDefinition { Height = new GridLength (50), MaxHeight = 40, MinHeight = 60 });
			grid.AddChild (new MyContentControl (50, 50), 0, 0, 0, 0);
			CreateAsyncTest (grid, () => {
				Assert.AreEqual (60, grid.RowDefinitions [0].ActualHeight, "#1");
			});
		}

		[TestMethod]
		[Asynchronous]
		public void SizeExceedsBounds2 ()
		{
			Grid grid = new Grid ();
			grid.RowDefinitions.Add (new RowDefinition { Height = new GridLength (50), MaxHeight = 60, MinHeight = 40 });
			grid.RowDefinitions.Add (new RowDefinition { Height = new GridLength (50), MaxHeight = 60, MinHeight = 40 });
			grid.AddChild (new MyContentControl (100, 1000), 0, 0, 0, 0);
			CreateAsyncTest (grid,
				() => {
					Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#1");
					grid.ChangeRowSpan (0, 2);
				}, () => {
					Assert.AreEqual (50, grid.RowDefinitions [0].ActualHeight, "#1");
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarAutoConstrainedGrid ()
		{
			MyGrid g = new MyGrid { Width = 170, Height = 170 };
			g.AddRows (GridLength.Auto, new GridLength (1, GridUnitType.Star));
			g.AddColumns (GridLength.Auto, new GridLength (1, GridUnitType.Star));

			g.AddChild (ContentControlWithChild (), 0, 1, 1, 1);
			g.AddChild (ContentControlWithChild (), 1, 0, 1, 1);
			g.AddChild (ContentControlWithChild (), 1, 1, 1, 1);
			g.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			foreach (MyContentControl child in g.Children) {
				Assert.AreEqual (0, child.ActualHeight, "height");
				Assert.AreEqual (0, child.ActualWidth, "height");

				Rectangle content = (Rectangle) child.Content;
				Assert.AreEqual (50, content.ActualHeight, "content height");
				Assert.AreEqual (50, content.ActualWidth, "content width");
			}

			CreateAsyncTest (g, () => {
				g.CheckFinalMeasureArg ("#1",
					new Size (120, inf), new Size (inf, 120),
					new Size (120, 120), new Size (inf, inf));

			});
		}

		[TestMethod]
		public void StarAutoConstrainedGrid2 ()
		{
			MyGrid g = new MyGrid { Width = 170, Height = 170 };
			g.AddRows (GridLength.Auto, new GridLength (1, GridUnitType.Star));
			g.AddColumns (GridLength.Auto, new GridLength (1, GridUnitType.Star));

			g.AddChild (ContentControlWithChild (), 0, 1, 1, 1);
			g.AddChild (ContentControlWithChild (), 1, 0, 1, 1);
			g.AddChild (ContentControlWithChild (), 1, 1, 1, 1);
			g.AddChild (ContentControlWithChild (), 0, 0, 1, 1);

			foreach (MyContentControl child in g.Children) {
				Assert.AreEqual (0, child.ActualHeight, "height");
				Assert.AreEqual (0, child.ActualWidth, "height");

				Rectangle content = (Rectangle)child.Content;
				Assert.AreEqual (50, content.ActualHeight, "content height");
				Assert.AreEqual (50, content.ActualWidth, "content width");
			}
			g.Measure (new Size (170, 170));
			g.CheckFinalMeasureArg ("#1",
					new Size (120, inf), new Size (inf, 120),
					new Size (120, 120), new Size (inf, inf));
		}

		[TestMethod]
		public void StarAutoIsNotInfinite ()
		{
			var child1 =new MyContentControl { };
			var child2 = new MyContentControl { };
			MyGrid grid = new MyGrid ();
			grid.AddRows (Auto, Auto, Auto, Star);
			grid.AddColumns (Auto, Star);

			grid.AddChild (child1, 0, 0, 1, 1);
			grid.AddChild (child2, 0, 0, 4, 2);

			grid.Measure (new Size (100, 100));
			Assert.AreEqual (Infinity, child1.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (100, 100), child2.MeasureOverrideArg, "#2");
		}

		[TestMethod]
		[Asynchronous]
		public void StarRows ()
		{
			GridUnitType star = GridUnitType.Star;
			MyGrid grid = new MyGrid { Name = "TESTER", Width = 100, Height = 210 };
			grid.AddRows (new GridLength (1, star), new GridLength (2, star));
			grid.AddChild (new MyContentControl (50, 50), 0, 0, 0, 0);
			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 70, 140);
					grid.CheckMeasureArgs ("#1a", new Size (100, 70));
					grid.AddRows (new GridLength (30));
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#2", 60, 120, 30);
					grid.CheckMeasureArgs ("#2a", new Size (100, 60));
					grid.Reset ();

					// Add a child to the fixed row
					grid.AddChild (new MyContentControl (50, 80), 2, 0, 0, 0);
				}, () => {
					grid.CheckRowHeights ("#3", 60, 120, 30);
					grid.CheckMeasureArgs ("#3a", new Size (100, 30));
					grid.Reset ();

					// Make the child span the last two rows
					grid.ChangeRow (1, 1);
					grid.ChangeRowSpan (1, 2);
				}, () => {
					grid.CheckRowHeights ("#4", 60, 120, 30);
					grid.CheckMeasureArgs ("#4a", new Size (100, 150));
					grid.Reset ();

					// Add another fixed row and move the large child to span both
					grid.AddRows (new GridLength (30));
					grid.ChangeRow (1, 2);
				}, () => {
					grid.CheckFinalMeasureArg ("#MeasureArgs", new Size (100, 50), new Size (100, 60));
					grid.CheckRowHeights ("#5", 50, 100, 30, 30);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRows2 ()
		{
			GridUnitType star = GridUnitType.Star;
			MyGrid grid = new MyGrid { Width = 100, Height = 210 };
			grid.AddRows (new GridLength (1, star), new GridLength (2, star));
			grid.AddChild (new MyContentControl (50, 50), 0, 0, 0, 0);
			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 70, 140);
					grid.CheckMeasureArgs ("#1b", new Size (100, 70));
					grid.AddRows (GridLength.Auto);

					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#2", 70, 140, 0);
					grid.CheckMeasureArgs ("#2b"); // MeasureOverride isn't called

					// Add a child to the fixed row
					grid.AddChild (new MyContentControl (50, 80), 2, 0, 0, 0);
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#3", 43, 87, 80);
					grid.CheckMeasureArgs ("#3b", new Size (100, inf), new Size (100, 43));
					grid.CheckMeasureOrder ("#3c", 1, 0);

					// Make the child span the last two rows
					grid.ChangeRow (1, 1);
					grid.ChangeRowSpan (1, 2);
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#4", 70, 140, 0);
					grid.CheckMeasureArgs ("#4b", new Size (100, 70), new Size (100, 140));
					grid.CheckMeasureOrder ("#4c", 0, 1);

					// Add another fixed row and move the large child to span both
					grid.AddRows (GridLength.Auto);
					grid.ChangeRow (1, 2);
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#5", 43, 87, 40, 40);
					grid.CheckMeasureArgs ("#5b", new Size (100, inf), new Size (100, 43));
					grid.CheckMeasureOrder ("#5c", 1, 0);
				}
			);
		}

		class PanelPoker : Panel
		{
			public List<Size> ArrangeArgs = new List<Size> ();
			public List<Size> ArrangeResults = new List<Size> ();

			public List<Size> MeasureArgs = new List<Size> ();
			public List<Size> MeasureResults = new List<Size> ();

			public MyGrid Grid { get; set; }

			public PanelPoker ()
			{
			}

			protected override Size ArrangeOverride (Size finalSize)
			{
				ArrangeArgs.Add (finalSize);
				Grid.Arrange (new Rect (0, 0, finalSize.Width, finalSize.Height));
				ArrangeResults.Add (new Size (Grid.ActualWidth, Grid.ActualHeight));
				return ArrangeResults.Last ();
			}

			protected override Size MeasureOverride (Size availableSize)
			{
				MeasureArgs.Add (availableSize);
				Grid.Measure (availableSize);
				MeasureResults.Add (Grid.DesiredSize);
				return MeasureResults.Last ();
			}
		}

		[TestMethod]
		[Asynchronous]
		public void StarRows3 ()
		{
			GridLength oneStar = new GridLength (1, GridUnitType.Star);
			MyGrid grid = new MyGrid ();
			grid.AddRows (oneStar, oneStar, oneStar);
			grid.AddColumns (oneStar, oneStar, oneStar);

			Canvas canvas = new Canvas { Width = 120, Height = 120 };
			canvas.Children.Add (grid);
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (canvas,
				() => { },
				() => {
					grid.CheckRowHeights ("#3", 0, 100, 0);

					grid.CheckMeasureArgs ("#1", Infinity);
					grid.CheckMeasureResult ("#2", new Size (100, 100));

					grid.CheckRowHeights ("#3", 0, 100, 0);
					grid.CheckArrangeArgs ("#4", new Size (100, 100));
					grid.CheckArrangeResult ("#5", new Size (100, 100));
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void StarRows3b ()
		{
			var canvas = new Canvas { Width = 120, Height = 120 };
			PanelPoker poker = new PanelPoker ();
			MyGrid grid = new MyGrid ();
			grid.AddRows (Star, Star, Star);
			grid.AddColumns (Star, Star, Star);

			canvas.Children.Add (poker);
			poker.Grid = grid;
			grid.AddChild (new MyContentControl (100, 100), 1, 1, 1, 1);

			CreateAsyncTest (canvas,
				() => { },
				() => {
					Assert.AreEqual (Infinity, poker.MeasureArgs [0], "#1");
					Assert.AreEqual (new Size (100, 100), poker.MeasureResults [0], "#2");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeArgs [0], "#3");
					Assert.AreEqual (new Size (100, 100), poker.ArrangeResults [0], "#4");

					grid.CheckRowHeights ("#5", 0, 100, 0);
					grid.CheckColWidths ("#6", 0, 100, 0);

					grid.CheckMeasureArgs ("#7", Infinity);
					grid.CheckMeasureResult ("#8", new Size (100, 100));

					grid.CheckArrangeArgs ("#9", new Size (100, 100));
					grid.CheckArrangeResult ("#10", new Size (100, 100));
				}
			);
		}

		[TestMethod]
		[MoonlightBug ("For some bizarre reason, calling Arrange here *does not* result in the children being arranged.")]
		public void StarRows5 ()
		{
			GridLength oneStar = new GridLength (1, GridUnitType.Star);
			MyGrid grid = new MyGrid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			grid.AddRows (oneStar, oneStar, oneStar);
			grid.AddColumns (oneStar, oneStar, oneStar);

			grid.AddChild (new MyContentControl (240, 240), 0, 0, 3, 3);
			grid.AddChild (new MyContentControl (150, 150), 0, 0, 1, 1);

			TestPanel.Children.Add (grid);
			grid.Measure (new Size (240, 240));
			grid.Arrange (new Rect (0, 0, 120, 120));

			grid.CheckRowHeights ("#1", 80, 80, 80);
			grid.CheckMeasureArgs ("#2", new Size (240, 240), new Size (80, 80));
			grid.CheckMeasureResult ("#3", new Size (240, 240), new Size (80, 80));
			grid.CheckDesired ("#4", new Size (240, 240), new Size (80, 80));
			grid.CheckMeasureOrder ("#5", 0, 1);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoRows ()
		{
			// This checks that rows expand to be large enough to hold the largest child
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto);

			grid.AddChild (new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 1, 1);
			grid.AddChild (new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 1, 1);

			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 60, 0);
					Grid.SetRow ((FrameworkElement) grid.Children [1], 1);
				}, () => {
					grid.CheckRowHeights ("#2", 50, 60);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoRows2 ()
		{
			// Start off with two elements in the first row with the smaller element having rowspan = 2
			// and see how rowspan affects the rendering.
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);

			grid.AddChild (new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 2, 1);
			grid.AddChild (new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 1, 1);

			// Start off with both elements at row 1, and the smaller element having rowspan = 2
			CreateAsyncTest (grid,
				() => {
					// If an element spans across multiple rows and one of those rows
					// is already large enough to contain that element, it puts itself
					// entirely inside that row
					grid.CheckRowHeights ("#1", 60, 0, 0);

					grid.ChangeRow (1, 1);
				}, () => {
					// An 'auto' row which has no children whose rowspan/colspan
					// *ends* in that row has a height of zero
					grid.CheckRowHeights ("#2", 0, 60, 0);
					grid.ChangeRow (1, 2);
				}, () => {
					// If an element which spans multiple rows is the only element in
					// the rows it spans, it divides evenly between the rows it spans
					grid.CheckRowHeights ("#2", 25, 25, 60);
					grid.ChangeRow (1, 0);
					grid.ChangeRow (0, 1);
				}, () => {
					grid.CheckRowHeights ("#2", 60, 25, 25);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoRows3 ()
		{
			// Start off with two elements in the first row with the larger element having rowspan = 2
			// and see how rowspan affects the rendering.
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto);

			grid.AddChild (new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 1, 1);
			grid.AddChild (new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 2, 1);

			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 55, 5, 0);
					grid.ChangeRow (1, 1);
				}, () => {
					grid.CheckRowHeights ("#2", 50, 30, 30);
					grid.ChangeRow (1, 2);
				}, () => {
					grid.CheckRowHeights ("#3", 50, 0, 60);
					grid.ChangeRow (1, 0);
					grid.ChangeRow (0, 1);
				}, () => {
					grid.CheckRowHeights ("#3", 5, 55, 0);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoRows4 ()
		{
			// See how rowspan = 3 affects this with 5 rows.
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);

			// Give first child a rowspan of 2
			grid.AddChild (new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 1, 1);
			grid.AddChild (new LayoutPoker { Width = 50, Height = 60 }, 0, 1, 3, 1);

			CreateAsyncTest (grid,
				() => {
					// If an element spans across multiple rows and one of those rows
					// is already large enough to contain that element, it puts itself
					// entirely inside that row
					grid.CheckRowHeights ("#1", 53.33, 3.33, 3.33, 0, 0);
					grid.ChangeRow (1, 1);
				}, () => {
					// An 'auto' row which has no children whose rowspan/colspan
					// *ends* in that row has a height of zero
					grid.CheckRowHeights ("#2", 50, 20, 20, 20, 0);
					grid.ChangeRow (1, 2);
				}, () => {
					// If an element which spans multiple rows is the only element in
					// the rows it spans, it divides evenly between the rows it spans
					grid.CheckRowHeights ("#3", 50, 0, 20, 20, 20);

					grid.ChangeRow (1, 0);
					grid.ChangeRow (0, 1);
				}, () => {
					// If there are two auto rows beside each other and an element spans those
					// two rows, the total height is averaged between the two rows.
					grid.CheckRowHeights ("#4", 3.33, 53.33, 3.33, 0, 0);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoRows5 ()
		{
			Grid grid = new Grid ();

			grid.AddColumns (new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);

			grid.AddChild (new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 3, 1);
			grid.AddChild (new LayoutPoker { Width = 50, Height = 60 }, 1, 0, 3, 1);

			// When calculating the heights of automatic rows, the children added to the grid
			// distribute their height in the opposite order in which they were added.
			CreateAsyncTest (grid,
				() => {
					// Here the element with height 60 distributes its height first
					grid.CheckRowHeights ("#1", 3.33, 23.33, 23.33, 20, 0);
					grid.ChangeRow (1, 1);

					grid.ChangeRow (0, 1);
					grid.ChangeRow (1, 0);
				}, () => {
					// Reversing the rows does not stop the '60' element from
					// Distributing its height first
					grid.CheckRowHeights ("#2", 20, 23.33, 23.33, 3.33, 0);

					// Now reverse the order in which the elements are added so that
					// the '50' element distributes first.
					grid.Children.Clear ();
					grid.AddChild (new LayoutPoker { Width = 50, Height = 60 }, 1, 0, 3, 1);
					grid.AddChild (new LayoutPoker { Width = 50, Height = 50 }, 0, 0, 3, 1);

				}, () => {
					grid.CheckRowHeights ("#3", 16.66, 25.55, 25.55, 8.88, 0);
					grid.ChangeRow (1, 1);

					grid.ChangeRow (0, 1);
					grid.ChangeRow (1, 0);
				}, () => {
					grid.CheckRowHeights ("#4", 16.66, 25.55, 25.55, 8.88, 0);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoAndFixedRows ()
		{
			MyGrid grid = new MyGrid ();

			grid.AddColumns (new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, new GridLength (15), GridLength.Auto, GridLength.Auto);

			grid.AddChild (new MyContentControl (50, 50), 0, 0, 3, 1);
			grid.AddChild (new MyContentControl (50, 60), 1, 0, 3, 1);

			// If an element spans multiple rows and one of them is *not* auto, it attempts to put itself
			// entirely inside that row
			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 0, 0, 60, 0, 0);
					grid.CheckMeasureArgs ("#1b", new Size (50, inf), new Size (50, inf));
					grid.CheckMeasureOrder ("#1c", 0, 1);

					// Forcing a maximum height on the fixed row makes it distribute
					// remaining height among the 'auto' rows.
					grid.RowDefinitions [2].MaxHeight = 15;
					grid.Reset ();
				}, () => {
					// Nothing needs to get re-measured, but the heights are redistributed as expected.
					grid.CheckRowHeights ("#2", 6.25, 28.75, 15, 22.5, 0);
					grid.CheckMeasureArgs ("#2b");
					grid.CheckMeasureOrder ("#2c");

					grid.RowDefinitions.Clear ();
					grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength (15), GridLength.Auto);
					grid.Reset ();
				}, () => {
					// Once again there's no remeasuring, just redistributing.
					grid.CheckRowHeights ("#3", 16.66, 16.66, 16.66, 60, 0);
					grid.CheckMeasureArgs ("#3b");
					grid.CheckMeasureOrder ("#3c");
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoAndFixedRows2 ()
		{
			TestPanel.Width = 200;
			TestPanel.Height = 1000;

			MyGrid grid = new MyGrid ();
			grid.AddColumns (new GridLength (50), new GridLength (50), new GridLength (50));
			grid.AddRows (new GridLength (30), new GridLength (40), GridLength.Auto, new GridLength (50));
			grid.AddChild (new MyContentControl (600, 600), 0, 0, 4, 4);
			grid.AddChild (new MyContentControl (80, 70), 0, 1, 1, 1);
			grid.AddChild (new MyContentControl (50, 60), 1, 0, 1, 1);
			grid.AddChild (new MyContentControl (10, 500), 1, 1, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckRowHeights ("#1", 190, 200, 0, 210);
				grid.CheckMeasureArgs ("#2",
											new Size (150, double.PositiveInfinity),
											new Size (50, 30),
											new Size (50, 40),
											new Size (50, 40));
				grid.CheckMeasureOrder ("#3", 0, 1, 2, 3);
			});
		}

		[TestMethod]
		[Asynchronous]
		public void AutoAndFixedRows3 ()
		{
			Grid grid = new Grid { Width = 10, Height = 10 };
			grid.AddColumns (new GridLength (50), new GridLength (50));
			grid.AddRows (new GridLength (20), new GridLength (20));

			grid.AddChild (new MyContentControl (50, 50), 0, 1, 2, 1);

			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 20, 20);
					grid.RowDefinitions.Insert (0, new RowDefinition { Height = GridLength.Auto });
				}, () => {
					grid.CheckRowHeights ("#2", 0, 50, 20);
					grid.RowDefinitions [1].MaxHeight = 35;
				}, () => {
					grid.CheckRowHeights ("#3", 15, 35, 20);
					grid.RowDefinitions [1].MaxHeight = 20;
					grid.ChangeRowSpan (0, 4);
				}, () => {
					grid.CheckRowHeights ("#4", 0, 20, 30);
					grid.AddRows (new GridLength (20));
				}, () => {
					grid.CheckRowHeights ("#5", 0, 20, 20, 20);
				}
			);
		}

		[TestMethod]
		[Asynchronous]
		public void AutoAndStarRows ()
		{
			TestPanel.MaxHeight = 160;
			MyGrid grid = new MyGrid ();

			grid.AddColumns (new GridLength (50));
			grid.AddRows (GridLength.Auto, GridLength.Auto, new GridLength (1, GridUnitType.Star), GridLength.Auto, GridLength.Auto);

			grid.AddChild (new MyContentControl (50, 50), 0, 0, 3, 1);
			grid.AddChild (new MyContentControl (50, 60), 1, 0, 3, 1);

			// Elements will put themselves entirely inside a 'star' row if they ca
			CreateAsyncTest (grid,
				() => {
					grid.CheckRowHeights ("#1", 0, 0, 160, 0, 0);
					grid.CheckMeasureArgs ("#1b", new Size (50, 160), new Size (50, 160));
					grid.CheckMeasureOrder ("#1c", 0, 1);

					// Forcing a maximum height on the star row doesn't spread
					// remaining height among the auto rows.
					grid.RowDefinitions [2].MaxHeight = 15;
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#2", 0, 0, 15, 0, 0);
					grid.CheckMeasureArgs ("#2b", new Size (50, 15), new Size (50, 15));
					grid.CheckMeasureOrder ("#2c", 0, 1);

					grid.RowDefinitions.Clear ();
					grid.AddRows (GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength (1, GridUnitType.Star), GridLength.Auto);
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#3", 16.66, 16.66, 16.66, 110, 0);
					grid.CheckMeasureArgs ("#3b", new Size (50, inf), new Size (50, 143.333));
					grid.CheckMeasureOrder ("#3c", 0, 1);

					grid.RowDefinitions [3].MaxHeight = 15;
					grid.Reset ();
				}, () => {
					grid.CheckRowHeights ("#4", 16.66, 16.66, 16.66, 15, 0);
					grid.CheckMeasureArgs ("#4b", new Size (50, 48.333));
					grid.CheckMeasureOrder ("#4c", 1);
				}
			);
		}


		[TestMethod]
		[Asynchronous]
		public void AutoCol_Empty_MaxWidth ()
		{
			// Ensure MaxWidth is respected in an empty Auto segment
			var grid = new Grid ();
			grid.AddColumns (Auto, Star);
			grid.ColumnDefinitions [0].MaxWidth = 10;
			grid.AddChild (ContentControlWithChild (), 0, 1, 0, 0);

			CreateAsyncTest(grid,() => {
				grid.UpdateLayout();
				Assert.AreEqual(0, grid.ColumnDefinitions[0].ActualWidth, "#1");
			});
		}

		[TestMethod]
		[Asynchronous]
		public void AutoCol_Empty_MinWidth ()
		{
			// Ensure MinWidth is respected in an empty Auto segment
			var grid = new Grid();
			grid.AddColumns(Auto, Star);
			grid.ColumnDefinitions[0].MinWidth = 10;
			grid.AddChild(ContentControlWithChild(), 0, 1, 0, 0);

			CreateAsyncTest(grid, () => {
				grid.UpdateLayout();
				Assert.AreEqual(10, grid.ColumnDefinitions[0].ActualWidth, "#1");
			});
		}

		[TestMethod]
		[Asynchronous]
		public void AutoCol_MaxWidth ()
		{
			// MaxWidth is *not* respected in an Auto segment
			var grid = new Grid();
			grid.AddColumns(Auto, Star);
			grid.ColumnDefinitions[0].MaxWidth = 10;
			grid.AddChild(ContentControlWithChild(), 0, 0, 0, 0);

			CreateAsyncTest(grid, () => {
				grid.UpdateLayout();
				Assert.AreEqual(50, grid.ColumnDefinitions[0].ActualWidth, "#1");
			});
		}

		[TestMethod]
		[Asynchronous]
		public void AutoCol_MinWidth ()
		{
			var grid = new Grid ();
			grid.AddColumns(Auto, Star);
			grid.ColumnDefinitions[0].MinWidth = 10;
			grid.AddChild(ContentControlWithChild(), 0, 0, 0, 0);

			CreateAsyncTest(grid, () => {
				grid.UpdateLayout();
				Assert.AreEqual(50, grid.ColumnDefinitions[0].ActualWidth, "#1");
			});
		}
	}

	class MyContentControl : ContentControl
	{
		public bool IsArranged { get; private set; }
		public bool IsMeasured { get; private set; }
		public Action<Size> ArrangeHook = delegate { };
		public Action<Size> MeasureHook = delegate { };
		public Size MeasureOverrideArg;
		public Size ArrangeOverrideArg;
		public Size MeasureOverrideResult;
		public Size ArrangeOverrideResult;

		public MyContentControl ()
		{
		}

		public MyContentControl (int width, int height)
		{
			Content = new Rectangle { Width = width, Height = height, Fill = new SolidColorBrush (Colors.Green) };
		}

		protected override Size ArrangeOverride (Size finalSize)
		{
			IsArranged = true;
			MyGrid grid = Parent as MyGrid;
			if (grid != null)
				grid.ArrangedElements.Add (new KeyValuePair<MyContentControl, Size> (this, finalSize));

			ArrangeOverrideArg = finalSize;
			if (ArrangeHook != null)
				ArrangeHook(finalSize);

			ArrangeOverrideResult = base.ArrangeOverride (finalSize);

			if (grid != null)
				grid.ArrangeResultElements.Add (new KeyValuePair<MyContentControl, Size> (this, ArrangeOverrideResult));

			return ArrangeOverrideResult;
		}

		protected override Size MeasureOverride (Size availableSize)
		{
			IsMeasured = true;
			MyGrid grid = Parent as MyGrid;
			if (grid != null)
				grid.MeasuredElements.Add (new KeyValuePair<MyContentControl, Size> (this, availableSize));

			MeasureOverrideArg = availableSize;
			if (MeasureHook != null)
				MeasureHook(availableSize);

			MeasureOverrideResult = base.MeasureOverride (availableSize);
			
			if (grid != null)
				((MyGrid) Parent).MeasureResultElements.Add (new KeyValuePair<MyContentControl, Size> (this, MeasureOverrideResult));

			return MeasureOverrideResult;
		}
	}
}
