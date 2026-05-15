using System;
using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;

namespace RenderDemo.Pages
{
    public class HitTestingPage : UserControl
    {
        private const int GroupColumns = 8;
        private const int GroupRows = 5;
        private const int CellsPerGroupSide = 10;
        private const int CellStride = 10;
        private const int CellSize = 8;
        private const int AnimationTravel = 64;

        private readonly Canvas _scene;
        private readonly TextBlock _stats;
        private readonly Cell[] _cells = new Cell[GroupColumns * GroupRows * CellsPerGroupSide * CellsPerGroupSide];
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private Compositor? _compositor;
        private int _hitTestsPerFrame = 256;
        private int _updateCount;
        private int _hitTestCount;
        private int _hitCount;
        private int _lastSecondUpdateCount;
        private int _lastSecondHitTestCount;
        private int _lastSecondHitCount;
        private TimeSpan _lastSecondTime;
        private double _lastSecondUpdatesPerSecond;
        private double _lastSecondHitTestsPerSecond;
        private double _lastSecondHitsPerSecond;
        private int _clickCount;
        private bool _isAttached;
        private bool _updateQueued;
        private bool _animationsStarted;
        private Cell? _lastHitCell;
        private Cell? _lastClickedCell;

        public HitTestingPage()
        {
            _scene = new Canvas
            {
                Width = GroupColumns * CellsPerGroupSide * CellStride,
                Height = GroupRows * CellsPerGroupSide * CellStride,
                Background = Brushes.Transparent
            };

            _stats = new TextBlock
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(12),
                Padding = new Thickness(8, 4),
                Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                Foreground = Brushes.Black,
                IsHitTestVisible = false
            };

            var numberOfHitTests = new NumericUpDown
            {
                Minimum = 0,
                Value = _hitTestsPerFrame,
                Width = 200,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            numberOfHitTests.ValueChanged += (s, e) =>
            {
                if (numberOfHitTests.Value.HasValue)
                {
                    _hitTestsPerFrame = (int)numberOfHitTests.Value.Value;
                }
            };

            var param = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Margin = new Thickness(12),
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Hit tests per update:" },
                    numberOfHitTests
                }
            };

            var root = new Grid
            {
                ClipToBounds = true,
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                },
            };

            Grid.SetRow(param, 0);
            root.Children.Add(param);
            Grid.SetRow(_stats, 1);
            root.Children.Add(_stats);
            Grid.SetRow(_scene, 2);
            root.Children.Add(_scene);

            Content = root;
            BuildScene();
            ResetState();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ResetState();
            _compositor = ElementComposition.GetElementVisual(this)?.Compositor;
            _isAttached = true;
            RequestNextUpdate();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _isAttached = false;
            _updateQueued = false;
            _compositor = null;
            ResetState();
            base.OnDetachedFromVisualTree(e);
        }

        private void BuildScene()
        {
            var index = 0;
            var groupSize = CellsPerGroupSide * CellStride;

            for (var groupY = 0; groupY < GroupRows; groupY++)
            {
                for (var groupX = 0; groupX < GroupColumns; groupX++)
                {
                    var group = new Canvas
                    {
                        Width = groupSize,
                        Height = groupSize,
                        Background = Brushes.Transparent
                    };
                    Canvas.SetLeft(group, groupX * groupSize);
                    Canvas.SetTop(group, groupY * groupSize);
                    _scene.Children.Add(group);

                    for (var y = 0; y < CellsPerGroupSide; y++)
                    {
                        for (var x = 0; x < CellsPerGroupSide; x++)
                        {
                            var cell = new Cell(index)
                            {
                                Width = CellSize,
                                Height = CellSize,
                                Background = CreateBrush(index),
                                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
                            };
                            cell.PointerPressed += OnCellPointerPressed;

                            Canvas.SetLeft(cell, x * CellStride);
                            Canvas.SetTop(cell, y * CellStride);
                            group.Children.Add(cell);
                            _cells[index++] = cell;
                        }
                    }
                }
            }
        }

        private void OnCompositionUpdate()
        {
            _updateQueued = false;

            if (!_isAttached)
                return;

            if (!_animationsStarted)
                StartAnimations();

            RunHitTests();

            _updateCount++;
            if (_stopwatch.Elapsed - _lastSecondTime >= TimeSpan.FromSeconds(1))
                UpdateStats();

            RequestNextUpdate();
        }

        private void RequestNextUpdate()
        {
            if (_updateQueued || _compositor == null)
                return;

            _updateQueued = true;
            _compositor.RequestCompositionUpdate(OnCompositionUpdate);
        }

        private void StartAnimations()
        {
            var started = 0;
            var easing = new SineEaseInOut();

            for (var i = 0; i < _cells.Length; i++)
            {
                if (i % 5 != 0)
                    continue;

                var visual = ElementComposition.GetElementVisual(_cells[i]);
                if (visual == null)
                    continue;

                var translation = visual.Compositor.CreateVector3KeyFrameAnimation();
                translation.Target = "Translation";
                translation.Duration = TimeSpan.FromMilliseconds(900 + (i % 700));
                translation.Direction = PlaybackDirection.Alternate;
                translation.IterationBehavior = AnimationIterationBehavior.Forever;
                translation.InsertKeyFrame(0f, new Vector3(0, 0, 0), easing);
                translation.InsertKeyFrame(1f, GetAnimationOffset(i), easing);
                visual.StartAnimation("Translation", translation);

                started++;
            }

            _animationsStarted = started > 0;
        }

        private void StopAnimations()
        {
            for (var i = 0; i < _cells.Length; i++)
            {
                var visual = ElementComposition.GetElementVisual(_cells[i]);
                if (visual == null)
                    continue;

                visual.StopAnimation("Translation");
                visual.Translation = default;
            }

            _animationsStarted = false;
        }

        private void RunHitTests()
        {
            var width = Math.Max(1, _scene.Bounds.Width);
            var height = Math.Max(1, _scene.Bounds.Height);
            var baseIndex = _updateCount * 37;

            for (var i = 0; i < _hitTestsPerFrame; i++)
            {
                _hitTestCount++;
                var sample = baseIndex + (i * 97);
                var point = new Point(sample * 17 % width, sample * 29 % height);
                var hit = _scene.GetVisualAt(point);

                if (hit is Cell cell)
                {
                    SetLastHitCell(cell);
                    _hitCount++;
                }
            }
        }

        private static Vector3 GetAnimationOffset(int index)
        {
            var x = index % 4 switch
            {
                0 => -AnimationTravel,
                1 => AnimationTravel,
                2 => -AnimationTravel / 2,
                _ => AnimationTravel / 2
            };
            var y = index / 4 % 4 switch
            {
                0 => -AnimationTravel,
                1 => AnimationTravel,
                2 => AnimationTravel / 2,
                _ => -AnimationTravel / 2
            };

            return new Vector3(x, y, 0);
        }

        private void ResetState()
        {
            StopAnimations();

            _lastClickedCell?.ClearHighlight();
            _lastHitCell = null;
            _lastClickedCell = null;

            _updateCount = 0;
            _hitTestCount = 0;
            _hitCount = 0;
            _lastSecondUpdateCount = 0;
            _lastSecondHitTestCount = 0;
            _lastSecondHitCount = 0;
            _lastSecondTime = default;
            _lastSecondUpdatesPerSecond = 0;
            _lastSecondHitTestsPerSecond = 0;
            _lastSecondHitsPerSecond = 0;
            _clickCount = 0;
            _stopwatch.Restart();
            UpdateStats();
        }

        private void UpdateStats()
        {
            var elapsed = _stopwatch.Elapsed;
            var seconds = Math.Max(0.001, (elapsed - _lastSecondTime).TotalSeconds);
            _lastSecondUpdatesPerSecond = (_updateCount - _lastSecondUpdateCount) / seconds;
            _lastSecondHitTestsPerSecond = (_hitTestCount - _lastSecondHitTestCount) / seconds;
            _lastSecondHitsPerSecond = (_hitCount - _lastSecondHitCount) / seconds;
            _lastSecondUpdateCount = _updateCount;
            _lastSecondHitTestCount = _hitTestCount;
            _lastSecondHitCount = _hitCount;
            _lastSecondTime = elapsed;

            _stats.Text =
                $"Visuals: {_cells.Length} ({_cells.Length / 5} animated), " +
                $"Hit tests/frame: {_hitTestsPerFrame}, " +
                $"Composition updates/s: {_lastSecondUpdatesPerSecond:F1}, Hit tests/s: {_lastSecondHitTestsPerSecond:F0}, " +
                $"Hits/s: {_lastSecondHitsPerSecond:F0}, Misses/s: {_lastSecondHitTestsPerSecond - _lastSecondHitsPerSecond:F0}, " +
                $"Clicks: {_clickCount}";
        }

        private void OnCellPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Cell cell)
                return;

            SetLastClickedCell(cell);
            _clickCount++;
            e.Handled = true;
        }

        private void SetLastHitCell(Cell cell)
        {
            if (ReferenceEquals(_lastHitCell, cell))
                return;

            _lastHitCell = cell;
        }

        private void SetLastClickedCell(Cell cell)
        {
            if (!ReferenceEquals(_lastClickedCell, cell) && _lastClickedCell != null)
            {
                _lastClickedCell.IsLatestClick = false;
                _lastClickedCell.UpdateHighlight();
            }

            _lastClickedCell = cell;
            cell.IsLatestClick = true;
            cell.UpdateHighlight();
        }

        private static IBrush CreateBrush(int index)
        {
            var r = (byte)(80 + (index * 47 % 160));
            var g = (byte)(80 + (index * 91 % 160));
            var b = (byte)(80 + (index * 137 % 160));
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        private sealed class Cell : Border
        {
            public Cell(int index)
            {
                Index = index;
                BorderThickness = new Thickness(1);
            }

            public int Index { get; }
            public bool IsLatestClick { get; set; }

            public void ClearHighlight()
            {
                IsLatestClick = false;
                UpdateHighlight();
            }

            public void UpdateHighlight()
            {
                BorderBrush = IsLatestClick ? Brushes.White : Brushes.Transparent;
                ZIndex = IsLatestClick ? 1 : 0;
            }
        }
    }
}
