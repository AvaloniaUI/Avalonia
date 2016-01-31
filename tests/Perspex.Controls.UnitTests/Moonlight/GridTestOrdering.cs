using System.Collections.Generic;
using Perspex.Media;

using UIElement = Perspex.Controls.IControl;

namespace Perspex.Controls.UnitTests.Moonlight
{
    public partial class GridTest
	{
		static readonly GridLength Auto = GridLength.Auto;
		static readonly GridLength Star = new GridLength (1, GridUnitType.Star);
		//static readonly GridLength Pixel = new GridLength (30, GridUnitType.Pixel);
		static readonly double inf = double.PositiveInfinity;

		MyContentControl ContentControlWithChild ()
		{
			return new MyContentControl { Content = new Rectangle { Width = 50, Height = 50, Fill = new SolidColorBrush (Colors.Red) } };
		}

		[TestMethod]
		public void SizeDistributionOrder ()
		{
			MyGrid grid = new MyGrid ();
			grid.AddRows (Auto, Star, Auto);
			grid.AddColumns (Star, Auto, Star);

			// Auto/Auto
			grid.AddChild (ContentControlWithChild (), 0, 1, 1, 1);
			// Star/Auto
			grid.AddChild (ContentControlWithChild (), 1, 1, 1, 1);

			// If there is no spanning involved, the heights do not get distributed
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#1", 0, 1);
			grid.CheckMeasureArgs ("#2", Infinity, new Size (inf, 150));

			// Span the Star/Auto down into the Auto/Auto segment too.
			grid.ChangeRowSpan (1, 2);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#3", 0, 1);
			grid.CheckMeasureArgs ("#4", Infinity, new Size (inf, 150));

			// Add in an Auto/Star to see what happens
			grid.AddChild (ContentControlWithChild (), 2, 0, 1, 1);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#5", 0, 1, 2, 1);
			grid.CheckMeasureArgs ("#6", Infinity, new Size (inf, inf), new Size (75, inf), new Size (inf, 150));
		}

		[TestMethod]
		public void AutoStarPriority ()
		{
			// Test a bunch of combinations of auto/star with/without span to see
			// which order things are measured in.
			MyGrid grid = new MyGrid { Width = 200, Height = 200 };
			grid.AddRows (Auto, Auto, Star, Auto, Star);
			grid.AddColumns (Auto, Star, Auto, Star, Auto);

			// Two auto-autos and one star-star
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);
			grid.AddChild (ContentControlWithChild (), 4, 3, 1, 1);

			// Measured in-order. star-star is last.
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#1", 0, 1, 2);
			grid.CheckMeasureArgs ("#1b", Infinity, Infinity, new Size (75, 75));

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#2", 1, 2, 0);
			grid.CheckMeasureArgs ("#2b", Infinity, Infinity, new Size (75, 75));

			// Span > 1 does not affect measure order. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (0, 2);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#3", 0, 1, 2);
			grid.CheckMeasureArgs ("#3b", Infinity, Infinity, new Size (75, 75));

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#4", 1, 2, 0);
			grid.CheckMeasureArgs ("#4b", Infinity, Infinity, new Size (75, 75));

			// Elements which do not span a star row are measured first. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (0, 3);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#5", 1, 0, 2);
			grid.CheckMeasureArgs ("#5b", Infinity, new Size (inf, 125), new Size (75, 75));

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#6", 1, 2, 0);
			grid.CheckMeasureArgs ("#6b", Infinity, new Size (inf, 125), new Size (75, 75));

			// Elements which do not span a star col are measured first. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (0, 1);
			grid.ChangeColSpan (0, 2);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#7", 1, 0, 2);
			grid.CheckMeasureArgs ("#7b", Infinity, new Size (125, inf), new Size (75, 75));

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#8", 1, 2, 0);
			grid.CheckMeasureArgs ("#8b", Infinity, new Size (125, inf), new Size (75, 75));

			// Elements which span a Star row are measured before ones spanning a Star col. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (1, 3);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#9", 1, 0, 1, 2);
			grid.CheckMeasureArgs ("#9b", Infinity, new Size (125, inf), new Size (inf, 125), new Size (75, 75));

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#10", 1, 2, 1, 0);
			grid.CheckMeasureArgs ("#10b", Infinity, new Size (125, inf), new Size (inf, 125), new Size (75, 75));

			// Auto/Auto is measured before all. star-star is last.
			grid.ReverseChildren ();
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#11", 3, 1, 0, 1, 2);
			grid.CheckMeasureArgs ("#11b", Infinity, Infinity, new Size (125, inf), new Size (inf, 125), new Size (75, 75));

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#12", 0, 2, 3, 2, 1);
			grid.CheckMeasureArgs ("#12b", Infinity, Infinity, new Size (125, inf), new Size (inf, 125), new Size (75, 75));
		}

		[TestMethod]
		public void AutoStarPriority2 ()
		{
			// Test a bunch of combinations of auto/star with/without span to see
			// which order things are measured in.
			MyGrid grid = new MyGrid ();
			grid.AddRows (Auto, Auto, Star, Auto, Star);
			grid.AddColumns (Auto, Star, Auto, Star, Auto);

			// Two auto-autos and one star-star
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);
			grid.AddChild (ContentControlWithChild (), 4, 3, 1, 1);

			// Measured in-order. star-star is last.
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#1", 0, 1, 2);

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#2", 1, 2, 0);

			// Span > 1 does not affect measure order. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (0, 2);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#3", 0, 1, 2);

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#4", 1, 2, 0);

			// Elements which do not span a star row are measured first. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (0, 3);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#5", 1, 0, 2);

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#6", 1, 2, 0);

			// Elements which do not span a star col are measured first. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (0, 1);
			grid.ChangeColSpan (0, 2);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#7", 1, 0, 2);

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#8", 1, 2, 0);

			// Elements which span a Star row are measured before ones spanning a Star col. star-star is last.
			grid.ReverseChildren ();
			grid.ChangeRowSpan (1, 3);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#9", 1, 0, 1, 2);

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#10", 1, 2, 1, 0);

			// Auto/Auto is measured before all. star-star is last.
			grid.ReverseChildren ();
			grid.AddChild (ContentControlWithChild (), 0, 0, 1, 1);
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#11", 3, 1, 0, 1, 2);

			grid.ReverseChildren ();
			grid.ResetAndInvalidate ();
			grid.Measure (new Size (200, 200));
			grid.CheckMeasureOrder ("#12", 0, 2, 3, 2, 1);
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder ()
		{
			TestPanel.Width = 500;
			TestPanel.Height = 500;

			MyGrid grid = new MyGrid ();
			grid.AddRows (new GridLength (1, GridUnitType.Auto), new GridLength (50), new GridLength (1, GridUnitType.Star));
			grid.AddColumns (new GridLength (1, GridUnitType.Auto), new GridLength (50), new GridLength (1, GridUnitType.Star));
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					grid.AddChild (ContentControlWithChild (), i, j, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 0, 1, 3, 4, 6, 2, 5, 6, 7, 8);
				grid.CheckMeasureArgs ("#2", new Size (inf, inf), new Size (50, inf), new Size (inf, 50),
											  new Size (50, 50), new Size (inf, inf), new Size (400, inf),
											  new Size (400, 50), new Size (inf, 400), new Size (50, 400), new Size (400, 400));
				grid.ReverseChildren ();
				grid.Reset ();
			}, () => {
				grid.CheckMeasureOrder ("#3", 4, 5, 7, 8, 2, 3, 6, 2, 0, 1);
				grid.CheckMeasureArgs ("#4", new Size (50, 50), new Size (inf, 50), new Size (50, inf),
											  new Size (inf, inf), new Size (inf, inf), new Size (400, 50),
											  new Size (400, inf), new Size (inf, 400), new Size (400, 400), new Size (50, 400));
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder2 ()
		{
			TestPanel.Width = 500;
			TestPanel.Height = 500;

			MyGrid grid = new MyGrid ();
			grid.AddRows (new GridLength (50), new GridLength (1, GridUnitType.Auto), new GridLength (1, GridUnitType.Star));
			grid.AddColumns (new GridLength (50), new GridLength (1, GridUnitType.Auto), new GridLength (1, GridUnitType.Star));
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					grid.AddChild (ContentControlWithChild (), i, j, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 0, 1, 3, 4, 7, 2, 5, 7, 6, 8);
				grid.CheckMeasureArgs ("#2", new Size (50, 50), new Size (inf, 50), new Size (50, inf),
											  new Size (inf, inf), new Size (inf, inf), new Size (400, 50),
											  new Size (400, inf), new Size (inf, 400), new Size (50, 400), new Size (400, 400));
				grid.ReverseChildren ();
				grid.Reset ();
			}, () => {
				grid.CheckMeasureOrder ("#3", 4, 5, 7, 8, 1, 3, 6, 1, 0, 2);
				grid.CheckMeasureArgs ("#4", new Size (inf, inf), new Size (50, inf), new Size (inf, 50),
											  new Size (50, 50), new Size (inf, inf), new Size (400, inf),
											  new Size (400, 50), new Size (inf, 400), new Size (400, 400), new Size (50, 400));
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder3 ()
		{
			TestPanel.Width = 500;
			TestPanel.Height = 500;

			MyGrid grid = new MyGrid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (50), new GridLength (1, GridUnitType.Auto));
			grid.AddColumns (new GridLength (1, GridUnitType.Star), new GridLength (50), new GridLength (1, GridUnitType.Auto));
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					grid.AddChild (ContentControlWithChild (), i, j, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 4, 5, 7, 8, 2, 3, 6, 2, 0, 1);
				grid.CheckMeasureArgs ("#2", new Size (50, 50), new Size (inf, 50), new Size (50, inf),
											  new Size (inf, inf), new Size (inf, inf), new Size (400, 50),
											  new Size (400, inf), new Size (inf, 400), new Size (400, 400), new Size (50, 400));
				grid.ReverseChildren ();
				grid.Reset ();
			}, () => {
				grid.CheckMeasureOrder ("#3", 0, 1, 3, 4, 6, 2, 5, 6, 7, 8);
				grid.CheckMeasureArgs ("#4", new Size (inf, inf), new Size (50, inf), new Size (inf, 50),
											  new Size (50, 50), new Size (inf, inf), new Size (400, inf),
											  new Size (400, 50), new Size (inf, 400), new Size (50, 400), new Size (400, 400));
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder4 ()
		{
			TestPanel.Width = 500;
			TestPanel.Height = 500;

			MyGrid grid = new MyGrid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Auto), new GridLength (50));
			grid.AddColumns (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Auto), new GridLength (50));
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					grid.AddChild (ContentControlWithChild (), i, j, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 4, 5, 7, 8, 1, 3, 6, 1, 0, 2);
				grid.CheckMeasureArgs ("#2", new Size (inf, inf), new Size (50, inf), new Size (inf, 50),
											  new Size (50, 50), new Size (inf, inf), new Size (400, inf),
											  new Size (400, 50), new Size (inf, 400), new Size (400, 400), new Size (50, 400));
				grid.ReverseChildren ();
				grid.Reset ();
			}, () => {
				grid.CheckMeasureOrder ("#3", 0, 1, 3, 4, 7, 2, 5, 7, 6, 8);
				grid.CheckMeasureArgs ("#4", new Size (50, 50), new Size (inf, 50), new Size (50, inf),
											  new Size (inf, inf), new Size (inf, inf), new Size (400, 50),
											  new Size (400, inf), new Size (inf, 400), new Size (50, 400), new Size (400, 400));
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder6 ()
		{
			TestPanel.Width = 500;
			TestPanel.Height = 500;

			MyGrid grid = new MyGrid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Auto), new GridLength (50), new GridLength (1, GridUnitType.Star), GridLength.Auto);
			grid.AddColumns (new GridLength (1, GridUnitType.Star), new GridLength (1, GridUnitType.Auto), new GridLength (1, GridUnitType.Star), new GridLength (50), GridLength.Auto);
			for (int i = 0; i < 5; i++)
				for (int j = 0; j < 5; j++)
					grid.AddChild (ContentControlWithChild (), i, j, 1, 1);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 6, 8, 9, 11, 13, 14, 21, 23, 24, 1, 4, 16, 19, 5, 7, 10, 12, 20, 22, 1, 4, 16, 19, 0, 2, 3, 15, 17, 18);
				grid.CheckMeasureArgs ("#2",
					new Size (inf, inf), new Size (50, inf), new Size (inf, inf), new Size (inf, 50), new Size (50, 50),
					new Size (inf, 50), new Size (inf, inf), new Size (50, inf), new Size (inf, inf), new Size (inf, inf),
					new Size (inf, inf), new Size (inf, inf), new Size (inf, inf), new Size (175, inf), new Size (175, inf),
					new Size (175, 50), new Size (175, 50), new Size (175, inf), new Size (175, inf), new Size (inf, 175),
					new Size (inf, 175), new Size (inf, 175), new Size (inf, 175), new Size (175, 175), new Size (175, 175),
					new Size (50, 175), new Size (175, 175), new Size (175, 175), new Size (50, 175));
				grid.ReverseChildren ();
				grid.Reset ();
			}, () => {
				grid.CheckMeasureOrder ("#3", 0, 1, 3, 10, 11, 13, 15, 16, 18, 5,
											  8, 20, 23, 2, 4, 12, 14, 17, 19, 5,
											  8, 20, 23, 6, 7, 9, 21, 22, 24);
				grid.CheckMeasureArgs ("#4",
					new Size (inf, inf), new Size (50, inf), new Size (inf, inf), new Size (inf, 50), new Size (50, 50),
					new Size (inf, 50), new Size (inf, inf), new Size (50, inf), new Size (inf, inf), new Size (inf, inf),
					new Size (inf, inf), new Size (inf, inf), new Size (inf, inf), new Size (175, inf), new Size (175, inf),
					new Size (175, 50), new Size (175, 50), new Size (175, inf), new Size (175, inf), new Size (inf, 175),
					new Size (inf, 175), new Size (inf, 175), new Size (inf, 175), new Size (50, 175), new Size (175, 175),
					new Size (175, 175), new Size (50, 175), new Size (175, 175), new Size (175, 175));
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder7 ()
		{
			GridUnitType star = GridUnitType.Star;
			MyGrid grid = new MyGrid { Name = "TESTER", Width = 100, Height = 210 };
			grid.AddRows (new GridLength (1, star), new GridLength (2, star), new GridLength (30), new GridLength (30));
			grid.AddChild (new MyContentControl (50, 50), 0, 0, 0, 0);
			grid.AddChild (new MyContentControl (50, 80), 2, 0, 2, 0);

			CreateAsyncTest (grid, 
				() => { },
				() => {
				grid.CheckFinalMeasureArg ("#1", new Size (100, 50), new Size (100, 60));
				grid.CheckRowHeights ("#2", 50, 100, 30, 30);
				grid.CheckMeasureOrder ("#3", 1, 0);
				grid.CheckMeasureArgs ("#4", new Size (100, 60), new Size (100, 50));
			});
		}

		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder7b ()
		{
			// Items which have no star row/col are measured first. Rowspan/Colspan is taken into account
			GridUnitType star = GridUnitType.Star;
			MyGrid grid = new MyGrid { Name = "TESTER", Width = 230, Height = 230 };
			grid.AddRows (new GridLength (1, star), new GridLength (30));
			grid.AddColumns (new GridLength (30), new GridLength (1, star));

			// Create a 2x2 grid containing a child for every valid combination
			// of row, column, rowspan and colspan
			for (int row = 0; row < 2; row++)
				for (int col = 0; col < 2; col++)
					for (int rowspan = row + 1; rowspan <= 2; rowspan++)
						for (int colspan = col + 1; colspan <= 2; colspan++)
							grid.AddChild (ContentControlWithChild (), row, col, rowspan, colspan);

			var o = grid.Children [6];
			grid.Children.RemoveAt (6);
			grid.Children.Add (o);
			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 8, 6, 7, 0, 1, 2, 3, 4, 5);
				grid.CheckMeasureArgs ("#2",
					new Size (30, 30), new Size (230, 30), new Size (200, 30),
					new Size (30, 200), new Size (230, 200), new Size (30, 230),
					new Size (230, 230), new Size (200, 200), new Size (200, 230));
			});
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureOrder8 ()
		{
			// Items which have no star row/col are measured first. Rowspan/Colspan is taken into account
			GridUnitType star = GridUnitType.Star;
			MyGrid grid = new MyGrid { Name = "TESTER", Width = 230, Height = 230 };
			grid.AddRows (new GridLength (1, star), GridLength.Auto);
			grid.AddColumns (GridLength.Auto, new GridLength (1, star));

			// Create a 2x2 grid containing a child for every valid combination
			// of row, column, rowspan and colspan
			for (int row = 0; row < 2; row++)
				for (int col = 0; col < 2; col++)
					for (int rowspan = row + 1; rowspan <= 2; rowspan++)
						for (int colspan = col + 1; colspan <= 2; colspan++)
							grid.AddChild (ContentControlWithChild (), row, col, rowspan, colspan);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 6, 0, 2, 7, 8, 0, 2, 1, 3, 4, 5);
				grid.CheckMeasureArgs ("#2",
					new Size (inf, inf), new Size (inf, inf), new Size (inf, inf),
					new Size (230, inf), new Size (180, inf), new Size (inf, 180),
					new Size (inf, 230), new Size (230, 180), new Size (230, 230),
					new Size (180, 180), new Size (180, 230));
			});
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureOrder8b ()
		{
			// Items which have no star row/col are measured first. Rowspan/Colspan is taken into account
			GridUnitType star = GridUnitType.Star;
			MyGrid grid = new MyGrid { Name = "TESTER", Width = 230, Height = 230 };
			grid.AddRows (GridLength.Auto, new GridLength (1, star));
			grid.AddColumns (new GridLength (1, star), GridLength.Auto);

			// Create a 2x2 grid containing a child for every valid combination
			// of row, column, rowspan and colspan
			for (int row = 0; row < grid.RowDefinitions.Count; row++)
				for (int col = 0; col < grid.ColumnDefinitions.Count; col++)
					for (int rowspan = 1; (row + rowspan) <= grid.RowDefinitions.Count; rowspan++)
						for (int colspan = 1; (col + colspan) <= grid.ColumnDefinitions.Count; colspan++)
							grid.AddChild (ContentControlWithChild (), row, col, rowspan, colspan);

			CreateAsyncTest (grid, () => {
				grid.CheckMeasureOrder ("#1", 4, 5, 8, 0, 1, 5, 8, 2, 3, 6, 7);
				grid.CheckMeasureArgs ("#2",
					new Size (inf, inf), new Size (inf, inf), new Size (inf, inf),
					new Size (180, inf), new Size (230, inf), new Size (inf, 230),
					new Size (inf, 180), new Size (180, 230), new Size (230, 230),
					new Size (180, 180), new Size (230, 180));
			});
		}

		[TestMethod]
		[Asynchronous]
		public void MeasureOrder5 ()
		{
			MeasureOrder5Impl (false);
		}
		[TestMethod]
		[Asynchronous]
		[MoonlightBug]
		public void MeasureOrder5b ()
		{
			MeasureOrder5Impl (true);
		}

		void MeasureOrder5Impl (bool checkOrder)
		{
			TestPanel.Width = 500;
			TestPanel.Height = 500;

			MyContentControl star = ContentControlWithChild ();
			MyContentControl pixel = ContentControlWithChild ();
			MyContentControl auto = ContentControlWithChild ();

			MyGrid grid = new MyGrid ();
			grid.AddRows (new GridLength (1, GridUnitType.Star), GridLength.Auto, new GridLength (30, GridUnitType.Pixel), new GridLength (30, GridUnitType.Pixel));
			grid.AddColumns (new GridLength (50, GridUnitType.Pixel));

			CreateAsyncTest (grid,
				() => {
					grid.AddChild (star, 0, 0, 1, 1);
					grid.AddChild (auto, 1, 0, 1, 1);
					grid.AddChild (pixel, 2, 0, 1, 1);

				}, () => {
					if (checkOrder) {
						grid.CheckMeasureOrder ("#1", 1, 2, 0);
						grid.CheckMeasureArgs ("#a", new Size (50, inf), new Size (50, 30), new Size (50, 390));
					}
					grid.CheckFinalMeasureArg ("#a2", new Size (50, 390), new Size (50, inf), new Size (50, 30));
					grid.CheckRowHeights ("#b", 390, 50, 30, 30);

					grid.Children.Clear ();
					grid.AddChild (star, 0, 0, 1, 1);
					grid.AddChild (pixel, 2, 0, 1, 1);
					grid.AddChild (auto, 1, 0, 1, 1);
					grid.Reset ();
				}, () => {
					if (checkOrder) {
						grid.CheckMeasureOrder ("#2", 1, 2, 0);
						grid.CheckMeasureArgs ("#c", new Size (50, 30), new Size (50, inf), new Size (50, 390));
					}
					grid.CheckFinalMeasureArg ("#c2", new Size (50, 390), new Size (50, 30), new Size (50, inf));
					grid.CheckRowHeights ("#d", 390, 50, 30, 30);

					grid.Children.Clear ();
					grid.AddChild (pixel, 2, 0, 1, 1);
					grid.AddChild (star, 0, 0, 1, 1);
					grid.AddChild (auto, 1, 0, 1, 1);
					grid.Reset ();
				}, () => {
					if (checkOrder) {
						grid.CheckMeasureOrder ("#3", 0, 2, 1);
						grid.CheckMeasureArgs ("#e", new Size (50, 30), new Size (50, inf), new Size (50, 390));
					}
					grid.CheckFinalMeasureArg ("#e2", new Size (50, 30), new Size (50, 390), new Size (50, inf));
					grid.CheckRowHeights ("#f", 390, 50, 30, 30);

					grid.Children.Clear ();
					grid.AddChild (pixel, 2, 0, 1, 1);
					grid.AddChild (auto, 1, 0, 1, 1);
					grid.AddChild (star, 0, 0, 1, 1);
					grid.Reset ();
				}, () => {
					if (checkOrder) {
						grid.CheckMeasureOrder ("#4", 0, 1, 2);
						grid.CheckMeasureArgs ("#g", new Size (50, 30), new Size (50, inf), new Size (50, 390));
					}
					grid.CheckFinalMeasureArg ("#g2", new Size (50, 30), new Size (50, inf), new Size (50, 390));
					grid.CheckRowHeights ("#h", 390, 50, 30, 30);

					grid.Children.Clear ();
					grid.AddChild (auto, 1, 0, 1, 1);
					grid.AddChild (pixel, 2, 0, 1, 1);
					grid.AddChild (star, 0, 0, 1, 1);
					grid.Reset ();
				}, () => {
					if (checkOrder) {
						grid.CheckMeasureOrder ("#5", 0, 1, 2);
						grid.CheckMeasureArgs ("#i", new Size (50, inf), new Size (50, 30), new Size (50, 390));
					}
					grid.CheckFinalMeasureArg ("#i2", new Size (50, inf), new Size (50, 30), new Size (50, 390));
					grid.CheckRowHeights ("#j", 390, 50, 30, 30);

					grid.Children.Clear ();
					grid.AddChild (auto, 1, 0, 1, 1);
					grid.AddChild (star, 0, 0, 1, 1);
					grid.AddChild (pixel, 2, 0, 1, 1);
					grid.Reset ();
				}, () => {
					if (checkOrder) {
						grid.CheckMeasureOrder ("#6", 0, 2, 1);
						grid.CheckMeasureArgs ("#k", new Size (50, inf), new Size (50, 30), new Size (50, 390));
					}
					grid.CheckRowHeights ("#l", 390, 50, 30, 30);
				}
			);
		}
	}

	class MyGrid : Grid
	{
		public List<KeyValuePair<MyContentControl, Size>> ArrangedElements = new List<KeyValuePair<MyContentControl, Size>> ();
		public List<KeyValuePair<MyContentControl, Size>> ArrangeResultElements = new List<KeyValuePair<MyContentControl, Size>> ();
		public List<KeyValuePair<MyContentControl, Size>> MeasuredElements = new List<KeyValuePair<MyContentControl, Size>> ();
		public List<KeyValuePair<MyContentControl, Size>> MeasureResultElements = new List<KeyValuePair<MyContentControl, Size>> ();

        public MyGrid()
        {
            HorizontalAlignment = Layout.HorizontalAlignment.Center;
            VerticalAlignment = Layout.VerticalAlignment.Center;
        }

		public void Reset ()
		{
			ArrangedElements.Clear ();
			ArrangeResultElements.Clear ();
			MeasuredElements.Clear ();
			MeasureResultElements.Clear ();
		}

		public void ReverseChildren ()
		{
			List<UIElement> children = new List<UIElement> (Children);
			children.Reverse ();
			Children.Clear ();
			children.ForEach (Children.Add);
		}
	}
}
