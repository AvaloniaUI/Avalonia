//
// Arranging Unit Tests
//
// Author:
//   Moonlight Team (moonlight-list@lists.ximian.com)
// 
// Copyright 2009 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using Perspex.Layout;

namespace Perspex.Controls.UnitTests.Moonlight
{
    [TestClass]
	public class ArrangingTest : SilverlightTest
	{
		static readonly Size infinity = new Size (double.PositiveInfinity, double.PositiveInfinity);

		[TestMethod]
		public void ConsumeMoreSpaceInArrange ()
		{
			// Pass in 100, 100 as both the Arrange and Measure args,
			// but return 200, 200 from ArrangeOverride to see what
			// kind of clip gets applied.
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = new Size (200, 200)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (200, 200), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (200, 200), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 100, 100), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void ConsumeMoreSpaceInArrange2 ()
		{
			// Pass in 100, 100 as both the Arrange and Measure args,
			// but return 200, 200 from ArrangeOverride to see what
			// kind of clip gets applied.
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 150, 150);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = new Size (200, 200)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (200, 200), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (200, 200), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 150, 150), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 150, 150), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void ConsumeMoreSpaceInArrange3 ()
		{
			// Pass in 100, 100 as both the Arrange and Measure args,
			// but return 200, 200 from ArrangeOverride to see what
			// kind of clip gets applied.
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 175, 175);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = new Size (150, 150),
				ArrangeOverrideResult = new Size (200, 200)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (175, 175), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (200, 200), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (200, 200), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 175, 175), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 175, 175), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void ConsumeMoreSpaceInArrange4 ()
		{
			// Pass in 100, 100 as both the Arrange and Measure args,
			// but return 200, 200 from ArrangeOverride to see what
			// kind of clip gets applied.
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 60,
				Height = 60,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = new Size (200, 200)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (60, 60), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (200, 200), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (200, 200), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (60, 60), poker.DesiredSize, "#5");

			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 60, 60), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void ConsumeMoreSpaceInArrange5 ()
		{
			// Pass in 100, 100 as both the Arrange and Measure args,
			// but return 200, 200 from ArrangeOverride to see what
			// kind of clip gets applied.
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 150, 150);

			LayoutPoker poker = new LayoutPoker {
				Width = 60,
				Height = 60,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = new Size (200, 200)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (60, 60), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (100, 100), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (200, 200), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (200, 200), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (60, 60), poker.DesiredSize, "#5");

			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 60, 60), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 150, 150), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void ConsumeMoreSpaceInArrange6 ()
		{
			// Pass in 100, 100 as both the Arrange and Measure args,
			// but return 200, 200 from ArrangeOverride to see what
			// kind of clip gets applied.
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 175, 175);

			LayoutPoker poker = new LayoutPoker {
				Width = 60,
				Height = 60,
				MeasureOverrideResult = new Size (150, 150),
				ArrangeOverrideResult = new Size (200, 200)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (60, 60), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (200, 200), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (200, 200), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (60, 60), poker.DesiredSize, "#5");

			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 60, 60), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 175, 175), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		[MoonlightBug]
		public void ArrangeNoMeasure ()
		{
			LayoutPoker poker = new LayoutPoker ();
			poker.Arrange (new Rect (0, 0, 100, 100));
			Assert.IsTrue (poker.Arranged, "#1");
			Assert.IsFalse (poker.Measured, "#2");
		}

		[TestMethod]
		public void ArrangeOverride_Constraints ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 50, 50);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = new Size (50, 50)
			};

			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (100, 100), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#2");
			Assert.AreEqual (new Size (50, 50), poker.ArrangeOverrideArg, "#3");
			Assert.AreEqual (new Size (50, 50), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), new Size (poker.ActualWidth, poker.ActualHeight), "#5");
			
			Assert.IsNull (LayoutInformation.GetLayoutClip (poker), "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), LayoutInformation.GetLayoutSlot (poker), "#7");
		}	

		[TestMethod]
		public void ArrangeOverride_Constraints2 ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = new Size (100, 100)
			};

			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (100, 100), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#2");
			Assert.AreEqual (new Size (100, 100), poker.ArrangeOverrideArg, "#3");
			Assert.AreEqual (new Size (100, 100), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), new Size (poker.ActualWidth, poker.ActualHeight), "#5");
			Assert.IsNull (LayoutInformation.GetLayoutClip (poker), "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void InfMeasure_Unconstrained ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (infinity, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void InfMeasure_Unconstrained_PosMargin_Offset ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (150, 0, 300, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Margin = new Thickness {
					Top = 10,
					Left = 10,
					Bottom = 3,
					Right = 3
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (infinity, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (287,200), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (213,213), poker.DesiredSize, "#5");
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 287, 87), geom.Rect, "#7");
			Assert.AreEqual (new Rect (150, 0, 300, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void InfMeasure_Unconstrained_NegMargin_Offset ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (150, 0, 300, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Margin = new Thickness {
					Top = 10,
					Left = -110,
					Bottom = 3,
					Right = -3
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (infinity, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (413,200), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (87,213), poker.DesiredSize, "#5");
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 413, 87), geom.Rect, "#7");
			Assert.AreEqual (new Rect (150, 0, 300, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void InfMeasure_Unconstrained_PosMargin ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Margin = new Thickness {
					Top = 10,
						Left = 10,
						Bottom = 3,
						Right = 3
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (infinity, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (213,213), poker.DesiredSize, "#5");
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 87, 87), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void InfMeasure_Unconstrained_NegMargin ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Margin = new Thickness {
					Top = -10,
						Left = 10,
						Bottom = 3,
						Right = 3
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (infinity, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (213,193), poker.DesiredSize, "#5");
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 87, 107), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void InfMeasure_Unconstrained_NoStretch ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (infinity, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (50, 50, 100, 100), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void InfMeasure_Constrained_Smaller ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void InfMeasure_Constrained_Smaller_NoStretch ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void InfMeasure_Constrained_Larger ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (200, 200), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (150, 150), poker.DesiredSize, "#5");


			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void InfMeasure_Constrained_Larger_NoStretch ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (infinity);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (150, 150), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (25, 25, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void LargerArrange_Constrained_Larger2 ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");


			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger2a ()
		{
			Size measureSize = new Size (50, 50);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger2b ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger2c ()
		{
			Size measureSize = new Size (150, 150);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (150, 150), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger2d ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (50, 50),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (150, 150), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 150, 150), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger3 ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (150,150),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger4 ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (200, 200),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (200, 200), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Larger5 ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = new Size (250, 250),
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (250, 250), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#8");
		}


		[TestMethod]
		public void LargerArrange_Unconstrained ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (200, 200), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#7");
		}
		
		[TestMethod]
		public void LargerArrange_Unconstrained_NoStretch ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void LargerArrange_Constrained_Smaller ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#7");
		}
		
		[TestMethod]
		public void LargerArrange_Constrained_Smaller_NoStretch ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void LargerArrange_Constrained_Larger_NoStretch ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 200, 200);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (100, 100), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 200, 200), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void LargerMeasure_Unconstrained ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");


			RectangleGeometry geom = LayoutInformation.GetLayoutClip (poker) as RectangleGeometry;
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerMeasure_Unconstrained_NoStretch ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (50, 50, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void LargerMeasure_Constrained_Smaller ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void LargerMeasure_Constrained_Smaller_NoStretch ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void LargerMeasure_Constrained_Larger ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (200, 200), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (150, 150), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void NegativeMargin ()
		{
			var grid = new Grid ();
			var poker = new LayoutPoker {
				Margin = new Thickness (-1, -1, -1, -1),
				MeasureOverrideResult = new Size (80, 80),
				ArrangeOverrideResult = new Size (80, 80)
			};

			grid.Children.Add (poker);

			grid.Measure (infinity);
			grid.Arrange (new Rect (0, 0, 50, 50));

			Assert.AreEqual (new Size (78, 78), grid.DesiredSize, "#1");
			Assert.AreEqual (new Size (78, 78), poker.DesiredSize, "#2");

			Assert.AreEqual (new Size (78, 78), new Size (grid.ActualWidth, grid.ActualHeight), "#3");
			Assert.AreEqual (new Size (80, 80), new Size (poker.ActualWidth, poker.ActualHeight), "#4");

			Assert.AreEqual (new Size (78, 78), grid.RenderSize, "#5");
			Assert.AreEqual (new Size (80, 80), poker.RenderSize, "#6");

			RectangleGeometry geom = LayoutInformation.GetLayoutClip (grid) as RectangleGeometry;
			Assert.IsNotNull (geom, "#7");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#8");
			Assert.IsNull (LayoutInformation.GetLayoutClip (poker), "#9");

			Assert.AreEqual (new Rect (0, 0, 50, 50), LayoutInformation.GetLayoutSlot (grid), "#10");
			Assert.AreEqual (new Rect (0, 0, 78, 78), LayoutInformation.GetLayoutSlot (poker), "#11");
		}

		[TestMethod]
		public void NegativeMargin_Scrollbar ()
		{
			ScrollBar bar = new ScrollBar {
				Margin = new Thickness (0, -1, -1, -1),
				Width = 18,
				ViewportSize = 100,
				Maximum = 1000
			};
			
			//MyContentControl c = new MyContentControl { Content = bar };

			// Ensure the default template is applied
			//TestPanel.Children.Add (bar);
			//TestPanel.Children.Clear ();

			bar.Measure (infinity);
			bar.Arrange (new Rect (0, 0, 40, 40));

			Assert.AreEqual (new Size (18, 42), new Size (bar.ActualWidth, bar.ActualHeight), "#1");
			Assert.AreEqual (new Size (17, 0), bar.DesiredSize, "#2");
			Assert.AreEqual (new Size (18, 42), bar.RenderSize, "#3");
			RectangleGeometry geom = LayoutInformation.GetLayoutClip (bar) as RectangleGeometry;
			Assert.IsNull (geom, "#4");
			Assert.AreEqual (new Rect (0, 0, 40, 40), LayoutInformation.GetLayoutSlot (bar), "#6");
		}
		
		[TestMethod]
		public void LargerMeasure_Constrained_Larger_NoStretch ()
		{
			Size measureSize = new Size (200, 200);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (150, 150), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (25, 25, 100, 100), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void SameMeasureAndArrange_Unconstrained ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}
		
		[TestMethod]
		public void SameMeasureAndArrange_Unconstrained_NoStretch ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (measureSize, poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void SameMeasureAndArrange_Constrained_Larger ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}
		
		[TestMethod]
		public void SameMeasureAndArrange_Constrained_Larger_NoStretch ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 150,
				Height = 150,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (150, 150), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (150, 150), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (measureSize, poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#7");
		}

		[TestMethod]
		public void SameMeasureAndArrange_Constrained_Smaller ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		[TestMethod]
		public void SameMeasureAndArrange_Constrained_Smaller2 ()
		{
			Size measureSize = new Size (50, 50);
			Rect arrangeRect = new Rect (0, 0, 50, 50);

			LayoutPoker poker = new LayoutPoker {
				Width = 100,
				Height = 100,
					MeasureOverrideResult = new Size (25,25),
					ArrangeOverrideResult = new Size (25,25)
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (100, 100), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (new Size (100, 100), poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (new Size (25, 25), new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (new Size (25, 25), poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), LayoutInformation.GetLayoutSlot (poker), "#8");
		}
		
		[TestMethod]
		public void SameMeasureAndArrange_Constrained_Smaller_NoStretch ()
		{
			Size measureSize = new Size (100, 100);
			Rect arrangeRect = new Rect (0, 0, 100, 100);

			LayoutPoker poker = new LayoutPoker {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 50,
				Height = 50,
				MeasureOverrideResult = measureSize,
				ArrangeOverrideResult = measureSize
			};
			poker.Measure (measureSize);
			poker.Arrange (arrangeRect);

			Assert.AreEqual (new Size (50, 50), poker.MeasureOverrideArg, "#1");
			Assert.AreEqual (measureSize, poker.ArrangeOverrideArg, "#2");
			Assert.AreEqual (measureSize, new Size (poker.ActualWidth, poker.ActualHeight), "#3");
			Assert.AreEqual (measureSize, poker.RenderSize, "#4");
			Assert.AreEqual (new Size (50, 50), poker.DesiredSize, "#5");
			
			RectangleGeometry geom = (RectangleGeometry) LayoutInformation.GetLayoutClip (poker);
			Assert.IsNotNull (geom, "#6");
			Assert.AreEqual (new Rect (0, 0, 50, 50), geom.Rect, "#7");
			Assert.AreEqual (new Rect (0, 0, 100, 100), LayoutInformation.GetLayoutSlot (poker), "#8");
		}

		class LayoutPoker : Panel
		{
			public bool Arranged { get; private set; }
			public bool Measured { get; private set; }

			public Size ArrangeOverrideArg {
				get; private set;
			}
			public Size ArrangeOverrideResult {
				get; set;
			}
			public Size MeasureOverrideArg {
				get; private set;
			}
			public Size MeasureOverrideResult {
				get; set;
			}

			protected override Size ArrangeOverride (Size finalSize)
			{
				Arranged = true;
				ArrangeOverrideArg = finalSize;
				return ArrangeOverrideResult;
			}

			protected override Size MeasureOverride (Size availableSize)
			{
				Measured = true;
				MeasureOverrideArg = availableSize;
				return MeasureOverrideResult;
			}
		}
	}
}

