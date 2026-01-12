using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace TextTestApp
{
    public partial class MainWindow : Window
    {
        private SelectionAdorner? _selectionAdorner;

        public MainWindow()
        {
            InitializeComponent();

            _selectionAdorner = new();
            _selectionAdorner.Stroke = Brushes.Red;
            _selectionAdorner.Fill = new SolidColorBrush(Colors.LightSkyBlue, 0.25);
            _selectionAdorner.IsHitTestVisible = false;
            AdornerLayer.SetIsClipEnabled(_selectionAdorner, false);
            AdornerLayer.SetAdorner(_rendering, _selectionAdorner);

            _rendering.TextLineChanged += OnShapeBufferChanged;
            OnShapeBufferChanged();
        }

        private void OnNewWindowClick(object? sender, RoutedEventArgs e)
        {
            MainWindow win = new MainWindow();
            win.Show();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                _rendering.InvalidateVisual();
                OnShapeBufferChanged();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (_hits.IsKeyboardFocusWithin && _hits.SelectedIndex != -1)
                {
                    _hits.SelectedIndex = -1;
                    e.Handled = true;
                }
                else if (_buffer.IsKeyboardFocusWithin && _buffer.SelectedIndex != -1)
                {
                    _buffer.SelectedIndex = -1;
                    e.Handled = true;
                }
            }

            base.OnKeyDown(e);
        }

        private void OnShapeBufferChanged(object? sender, EventArgs e) => OnShapeBufferChanged();
        private void OnShapeBufferChanged()
        {
            if (_selectionAdorner == null)
                return;

            ListBuffers();
            ListHits();

            Rect bounds = _rendering.LineRenderBounds;
            _selectionAdorner!.Transform = Matrix.CreateTranslation(bounds.X, bounds.Y);
        }

        private void ListBuffers()
        {
            for (int i = _buffer.ItemCount - 1; i >= 1; i--)
                _buffer.Items.RemoveAt(i);

            TextLine? textLine = _rendering.TextLine;
            if (textLine == null)
                return;

            double currentX = _rendering.LineRenderBounds.Left;
            foreach (TextRun run in textLine.TextRuns)
            {
                if (run is ShapedTextRun shapedRun)
                {
                    _buffer.Items.Add(new TextBlock
                    {
                        Text = $"{run.GetType().Name}: Bidi = {shapedRun.BidiLevel}, Font = {shapedRun.ShapedBuffer.GlyphTypeface.FamilyName}",
                        FontWeight = FontWeight.Bold,
                        Padding = new Thickness(10, 0),
                        Tag = run,
                    });

                    ListBuffer(textLine, shapedRun, ref currentX);
                }
                else
                    _buffer.Items.Add(new TextBlock
                    {
                        Text = run.GetType().Name,
                        FontWeight = FontWeight.Bold,
                        Padding = new Thickness(10, 0),
                        Tag = run
                    });
            }
        }

        private void ListHits()
        {
            for (int i = _hits.ItemCount - 1; i >= 1; i--)
                _hits.Items.RemoveAt(i);

            TextLine? textLine = _rendering.TextLine;
            if (textLine == null)
                return;

            for (int i = 0; i < textLine.Length; i++)
            {
                string? clusterText = _rendering.Text!.Substring(i, 1);
                string? clusterHex = ToHex(clusterText);

                var hit = new CharacterHit(i);
                var prevHit = textLine.GetPreviousCaretCharacterHit(hit);
                var nextHit = textLine.GetNextCaretCharacterHit(hit);
                var bkspHit = textLine.GetBackspaceCaretCharacterHit(hit);

                GridRow row = new GridRow { ColumnSpacing = 10 };
                row.Children.Add(new Control());
                row.Children.Add(new TextBlock { Text = $"{bkspHit.FirstCharacterIndex}+{bkspHit.TrailingLength}" });
                row.Children.Add(new TextBlock { Text = $"{prevHit.FirstCharacterIndex}+{prevHit.TrailingLength}" });
                row.Children.Add(new TextBlock { Text = i.ToString(), FontWeight = FontWeight.Bold });
                row.Children.Add(new TextBlock { Text = $"{nextHit.FirstCharacterIndex}+{nextHit.TrailingLength}" });
                row.Children.Add(new TextBlock { Text = clusterHex });
                row.Children.Add(new TextBlock { Text = clusterText });
                row.Children.Add(new TextBlock { Text = textLine.GetDistanceFromCharacterHit(hit).ToString() });
                row.Tag = i;

                _hits.Items.Add(row);
            }
        }

        private static readonly IBrush TransparentAliceBlue = new SolidColorBrush(0x0F0188FF);
        private static readonly IBrush TransparentAntiqueWhite = new SolidColorBrush(0x28DF8000);
        private void ListBuffer(TextLine textLine, ShapedTextRun shapedRun, ref double currentX)
        {
            ShapedBuffer buffer = shapedRun.ShapedBuffer;

            int lastClusterStart = -1;
            bool oddCluster = false;

            IReadOnlyList<GlyphInfo> glyphInfos = buffer;

            currentX += shapedRun.GlyphRun.BaselineOrigin.X;
            for (var i = 0; i < glyphInfos.Count; i++)
            {
                GlyphInfo info = glyphInfos[i];
                int clusterStart = info.GlyphCluster;
                int clusterLength = FindClusterLenghtAt(i);
                string? clusterText = _rendering.Text!.Substring(clusterStart, clusterLength);
                string? clusterHex = ToHex(clusterText);

                Border border = new Border();
                if (clusterStart == lastClusterStart)
                {
                    clusterText = clusterHex = null;
                }
                else
                {
                    oddCluster = !oddCluster;
                    lastClusterStart = clusterStart;
                }
                border.Background = oddCluster ? TransparentAliceBlue : TransparentAntiqueWhite;


                GridRow row = new GridRow { ColumnSpacing = 10 };
                row.Children.Add(new Control());
                row.Children.Add(new TextBlock { Text = clusterStart.ToString() });
                row.Children.Add(new TextBlock { Text = clusterText });
                row.Children.Add(new TextBlock { Text = clusterHex, TextWrapping = TextWrapping.Wrap });
                row.Children.Add(new Image { Source = CreateGlyphDrawing(shapedRun.GlyphRun.GlyphTypeface, FontSize, info), Margin = new Thickness(2) });
                row.Children.Add(new TextBlock { Text = info.GlyphIndex.ToString() });
                row.Children.Add(new TextBlock { Text = info.GlyphAdvance.ToString() });
                row.Children.Add(new TextBlock { Text = info.GlyphOffset.ToString() });

                Geometry glyph = GetGlyphOutline(shapedRun.GlyphRun.GlyphTypeface, shapedRun.GlyphRun.FontRenderingEmSize, info);
                Rect glyphBounds = glyph.Bounds;
                Rect offsetBounds = glyphBounds.Translate(new Vector(currentX + info.GlyphOffset.X, info.GlyphOffset.Y));

                TextBlock boundsBlock = new TextBlock { Text = offsetBounds.ToString() };
                ToolTip.SetTip(boundsBlock, "Origin bounds: " + glyphBounds);
                row.Children.Add(boundsBlock);

                border.Child = row;
                border.Tag = offsetBounds;
                _buffer.Items.Add(border);
                
                currentX += glyphInfos[i].GlyphAdvance;
            }

            int FindClusterLenghtAt(int index)
            {
                int cluster = glyphInfos[index].GlyphCluster;
                if (shapedRun.BidiLevel % 2 == 0)
                {
                    while (++index < glyphInfos.Count)
                        if (glyphInfos[index].GlyphCluster != cluster)
                            return glyphInfos[index].GlyphCluster - cluster;

                    return shapedRun.Length + glyphInfos[0].GlyphCluster - cluster;
                }
                else
                {
                    while (--index >= 0)
                        if (glyphInfos[index].GlyphCluster != cluster)
                            return glyphInfos[index].GlyphCluster - cluster;

                    return shapedRun.Length + glyphInfos[glyphInfos.Count - 1].GlyphCluster - cluster;
                }
            }
        }

        private IImage CreateGlyphDrawing(GlyphTypeface glyphTypeface, double emSize, GlyphInfo info)
        {
            return new DrawingImage { Drawing = new GeometryDrawing { Brush = Brushes.Black, Geometry = GetGlyphOutline(glyphTypeface, emSize, info) } };
        }

        private Geometry GetGlyphOutline(GlyphTypeface typeface, double emSize, GlyphInfo info)
        {
            // substitute for GlyphTypeface.GetGlyphOutline
            return new GlyphRun(typeface, emSize, new[] { '\0' }, [info]).BuildGeometry();
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            InteractiveLineControl lineControl = (InteractiveLineControl)sender;
            TextLayout textLayout = lineControl.TextLayout;
            Rect lineBounds = lineControl.LineRenderBounds;

            PointerPoint pointerPoint = e.GetCurrentPoint(lineControl);
            Point point = new Point(pointerPoint.Position.X - lineBounds.Left, pointerPoint.Position.Y - lineBounds.Top);
            _coordinates.Text = $"{pointerPoint.Position.X:F4}, {pointerPoint.Position.Y:F4}";

            TextHitTestResult textHit = textLayout.HitTestPoint(point);
            _hit.Text = $"{textHit.TextPosition} ({textHit.CharacterHit.FirstCharacterIndex}+{textHit.CharacterHit.TrailingLength})";
            if (textHit.IsTrailing)
                _hit.Text += " T";

            if (textHit.IsInside)
            {
                _hits.SelectedIndex = textHit.TextPosition + 1; // header
            }
            else
                _hits.SelectedIndex = -1;
        }

        private void OnHitTestMethodChanged(object? sender, RoutedEventArgs e)
        {
            _hits.SelectionMode = _hitRangeToggle.IsChecked == true ? SelectionMode.Multiple : SelectionMode.Single;
        }

        private void OnHitsSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_selectionAdorner == null)
                return;

            List<Rect> rectangles = new List<Rect>();
            TextLayout textLayout = _rendering.TextLayout;

            if (_hitRangeToggle.IsChecked == true)
            {
                // collect continuous selected indices
                List<(int start, int length)> selections = new(1);

                int[] indices = _hits.Selection.SelectedIndexes.ToArray();
                Array.Sort(indices);

                int currentIndex = -1;
                int currentLength = 0;
                for (int i = 0; i < indices.Length; i++)
                    if (_hits.Items[indices[i]] is Control { Tag: int index })
                    {
                        if (index == currentIndex + currentLength)
                        {
                            currentLength++;
                        }
                        else
                        {
                            if (currentLength > 0)
                                selections.Add((currentIndex, currentLength));

                            currentIndex = index;
                            currentLength = 1;
                        }
                    }

                if (currentLength > 0)
                    selections.Add((currentIndex, currentLength));

                foreach (var selection in selections)
                {
                    var selectionRectangles = textLayout.HitTestTextRange(selection.start, selection.length);
                    rectangles.AddRange(selectionRectangles);
                }
            }
            else
            {
                if (_hits.SelectedItem is Control { Tag: int index })
                {
                    Rect rect = textLayout.HitTestTextPosition(index);
                    rectangles.Add(rect);
                }
            }

            _selectionAdorner.Rectangles = rectangles;
        }

        private void OnBufferSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_selectionAdorner is null)
                return;

            var rectangles = new List<Rect>(_buffer.Selection.Count);

            if (_buffer.SelectedItems is { } selectedItems)
            {
                foreach (var row in selectedItems)
                    if (row is Control { Tag: Rect rect })
                        rectangles.Add(rect);
            }

            _selectionAdorner.Rectangles = rectangles;
        }

        private static string ToHex(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            return string.Join(" ", s.Select(c => ((int)c).ToString("X4")));
        }
    }
}
