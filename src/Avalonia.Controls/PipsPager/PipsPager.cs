using System;
using Avalonia.Threading;
using Avalonia.Controls.Metadata;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a control that lets the user navigate through a paginated collection using a set of pips.
    /// </summary>
    [TemplatePart("PART_PreviousButton", typeof(Button))]
    [TemplatePart("PART_NextButton", typeof(Button))]
    [TemplatePart("PART_PipsPagerList", typeof(ItemsControl))]
    [PseudoClasses(":first-page", ":last-page", ":vertical", ":horizontal")]
    public class PipsPager : TemplatedControl
    {
        private Button? _previousButton;
        private Button? _nextButton;
        private ItemsControl? _pipsPagerList;
        private bool _scrollPending;

        /// <summary>
        /// Defines the <see cref="MaxVisiblePips"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxVisiblePipsProperty =
            AvaloniaProperty.Register<PipsPager, int>(nameof(MaxVisiblePips), 5);

        /// <summary>
        /// Defines the <see cref="IsNextButtonVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsNextButtonVisibleProperty =
            AvaloniaProperty.Register<PipsPager, bool>(nameof(IsNextButtonVisible), true);

        /// <summary>
        /// Defines the <see cref="NumberOfPages"/> property.
        /// </summary>
        public static readonly StyledProperty<int> NumberOfPagesProperty =
            AvaloniaProperty.Register<PipsPager, int>(nameof(NumberOfPages));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<PipsPager, Orientation>(nameof(Orientation), Orientation.Horizontal);

        /// <summary>
        /// Defines the <see cref="IsPreviousButtonVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsPreviousButtonVisibleProperty =
            AvaloniaProperty.Register<PipsPager, bool>(nameof(IsPreviousButtonVisible), true);

        /// <summary>
        /// Defines the <see cref="SelectedPageIndex"/> property.
        /// </summary>
        public static readonly StyledProperty<int> SelectedPageIndexProperty =
            AvaloniaProperty.Register<PipsPager, int>(nameof(SelectedPageIndex));
        
        /// <summary>
        /// Defines the <see cref="TemplateSettings"/> property.
        /// </summary>
        public static readonly StyledProperty<PipsPagerTemplateSettings> TemplateSettingsProperty =
            AvaloniaProperty.Register<PipsPager, PipsPagerTemplateSettings>(nameof(TemplateSettings));

        /// <summary>
        /// Defines the <see cref="PreviousButtonStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> PreviousButtonStyleProperty =
            AvaloniaProperty.Register<PipsPager, ControlTheme?>(nameof(PreviousButtonStyle));

        /// <summary>
        /// Defines the <see cref="NextButtonStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> NextButtonStyleProperty =
            AvaloniaProperty.Register<PipsPager, ControlTheme?>(nameof(NextButtonStyle));

        /// <summary>
        /// Occurs when the selected index has changed.
        /// </summary>
        public event EventHandler<PipsPagerSelectedIndexChangedEventArgs>? SelectedIndexChanged;

        static PipsPager()
        {
            SelectedPageIndexProperty.Changed.AddClassHandler<PipsPager>((x, e) => x.OnSelectedPageIndexChanged(e));
            NumberOfPagesProperty.Changed.AddClassHandler<PipsPager>((x, e) => x.OnNumberOfPagesChanged(e));
            IsPreviousButtonVisibleProperty.Changed.AddClassHandler<PipsPager>((x, e) => x.OnIsPreviousButtonVisibleChanged(e));
            IsNextButtonVisibleProperty.Changed.AddClassHandler<PipsPager>((x, e) => x.OnIsNextButtonVisibleChanged(e));
            OrientationProperty.Changed.AddClassHandler<PipsPager>((x, e) => x.OnOrientationChanged(e));
            MaxVisiblePipsProperty.Changed.AddClassHandler<PipsPager>((x, e) => x.OnMaxVisiblePipsChanged(e));
        }

        public PipsPager()
        {
            SetValue(TemplateSettingsProperty, new PipsPagerTemplateSettings());
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Gets or sets the maximum number of visible pips.
        /// </summary>
        public int MaxVisiblePips
        {
            get => GetValue(MaxVisiblePipsProperty);
            set => SetValue(MaxVisiblePipsProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the next button.
        /// </summary>
        public bool IsNextButtonVisible
        {
            get => GetValue(IsNextButtonVisibleProperty);
            set => SetValue(IsNextButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of pages.
        /// </summary>
        public int NumberOfPages
        {
            get => GetValue(NumberOfPagesProperty);
            set => SetValue(NumberOfPagesProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation of the pips.
        /// </summary>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the previous button.
        /// </summary>
        public bool IsPreviousButtonVisible
        {
            get => GetValue(IsPreviousButtonVisibleProperty);
            set => SetValue(IsPreviousButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the current selected page index.
        /// </summary>
        public int SelectedPageIndex
        {
            get => GetValue(SelectedPageIndexProperty);
            set => SetValue(SelectedPageIndexProperty, value);
        }

        /// <summary>
        /// Gets the template settings.
        /// </summary>
        public PipsPagerTemplateSettings TemplateSettings
        {
            get => GetValue(TemplateSettingsProperty);
            set => SetValue(TemplateSettingsProperty, value);
        }

        /// <summary>
        /// Gets or sets the style for the previous button.
        /// </summary>
        public ControlTheme? PreviousButtonStyle
        {
            get => GetValue(PreviousButtonStyleProperty);
            set => SetValue(PreviousButtonStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style for the next button.
        /// </summary>
        public ControlTheme? NextButtonStyle
        {
            get => GetValue(NextButtonStyleProperty);
            set => SetValue(NextButtonStyleProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PipsPagerAutomationPeer(this);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Unsubscribe from previous button events
            if (_previousButton != null)
            {
                _previousButton.Click -= PreviousButton_Click;
            }

            if (_nextButton != null)
            {
                _nextButton.Click -= NextButton_Click;
            }

            // Unsubscribe from previous list events
            if (_pipsPagerList != null)
            {
                _pipsPagerList.SizeChanged -= OnPipsPagerListSizeChanged;
                _pipsPagerList.ContainerPrepared -= OnContainerPrepared;
                _pipsPagerList.ContainerIndexChanged -= OnContainerIndexChanged;
            }

            // Get template parts
            _previousButton = e.NameScope.Find<Button>("PART_PreviousButton");
            _nextButton = e.NameScope.Find<Button>("PART_NextButton");
            _pipsPagerList = e.NameScope.Find<ItemsControl>("PART_PipsPagerList");

            // Set up previous button
            if (_previousButton != null)
            {
                _previousButton.Click += PreviousButton_Click;
                AutomationProperties.SetName(_previousButton, "Previous page");
            }

            // Set up next button
            if (_nextButton != null)
            {
                _nextButton.Click += NextButton_Click;
                AutomationProperties.SetName(_nextButton, "Next page");
            }

            UpdateNavigationButtonIcons();

            // Set up pips list
            if (_pipsPagerList != null)
            {
                _pipsPagerList.SizeChanged += OnPipsPagerListSizeChanged;
                _pipsPagerList.ContainerPrepared += OnContainerPrepared;
                _pipsPagerList.ContainerIndexChanged += OnContainerIndexChanged;
            }

            UpdateButtonsState();
            UpdatePseudoClasses();
            UpdatePagerSize();
            RequestScrollToSelectedPip();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled) 
                return;

            if (Orientation == Orientation.Horizontal)
            {
                if (e.Key == Key.Left)
                {
                    if (SelectedPageIndex > 0)
                    {
                        SelectedPageIndex--;
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Right)
                {
                    if (SelectedPageIndex < NumberOfPages - 1)
                    {
                        SelectedPageIndex++;
                        e.Handled = true;
                    }
                }
            }
            else
            {
                if (e.Key == Key.Up)
                {
                    if (SelectedPageIndex > 0)
                    {
                        SelectedPageIndex--;
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Down)
                {
                    if (SelectedPageIndex < NumberOfPages - 1)
                    {
                        SelectedPageIndex++;
                        e.Handled = true;
                    }
                }
            }
        }

        private void OnSelectedPageIndexChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newIndex = (int)e.NewValue!;
            var oldIndex = (int)e.OldValue!;

            if (newIndex < 0)
            {
                SetCurrentValue(SelectedPageIndexProperty, 0);
                return;
            }

            if (NumberOfPages > 0)
            {
                if (newIndex >= NumberOfPages)
                {
                    SetCurrentValue(SelectedPageIndexProperty, NumberOfPages - 1);
                    return;
                }
            }
            else
            {
                if (newIndex > 0)
                {
                    SetCurrentValue(SelectedPageIndexProperty, 0);
                    return;
                }
            }
            
            UpdateButtonsState();
            UpdatePseudoClasses();
            RequestScrollToSelectedPip();

            SelectedIndexChanged?.Invoke(this, new PipsPagerSelectedIndexChangedEventArgs(oldIndex, newIndex));
        }

        private void OnNumberOfPagesChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var pips = TemplateSettings.Pips;
            var newValue = (int)e.NewValue!;

            if (newValue < 0)
            {
                return;
            }

            if (pips.Count < newValue)
            {
                for (int i = pips.Count; i < newValue; i++)
                {
                    pips.Add(i + 1);
                }
            }
            else if (pips.Count > newValue)
            {
                pips.RemoveRange(newValue, pips.Count - newValue);
            }

            if (SelectedPageIndex >= newValue && newValue > 0)
            {
                SetCurrentValue(SelectedPageIndexProperty, newValue - 1);
            }
            
            UpdateButtonsState();
            UpdatePseudoClasses();
            UpdatePagerSize();
            RequestScrollToSelectedPip();
        }
        
        private void OnIsPreviousButtonVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateButtonsState();
        }

        private void OnIsNextButtonVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateButtonsState();
        }

        private void OnOrientationChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdatePseudoClasses();
            UpdatePagerSize();
            UpdateNavigationButtonIcons();
        }

        private void OnMaxVisiblePipsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdatePagerSize();
        }

        private void PreviousButton_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedPageIndex > 0)
            {
                SelectedPageIndex--;
            }
        }

        private void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedPageIndex < NumberOfPages - 1)
            {
                SelectedPageIndex++;
            }
        }

        private void UpdateButtonsState()
        {
            if (_previousButton != null)
                _previousButton.IsEnabled = SelectedPageIndex > 0;
            
            if (_nextButton != null)
                _nextButton.IsEnabled = SelectedPageIndex < NumberOfPages - 1;
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":first-page", SelectedPageIndex == 0);
            PseudoClasses.Set(":last-page", SelectedPageIndex >= NumberOfPages - 1);
            PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        }

        private void OnPipsPagerListSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdatePagerSize();
        }

        private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            UpdateContainerAutomationProperties(e.Container, e.Index);
        }

        private void OnContainerIndexChanged(object? sender, ContainerIndexChangedEventArgs e)
        {
            UpdateContainerAutomationProperties(e.Container, e.NewIndex);
        }

        private void UpdateContainerAutomationProperties(Control container, int index)
        {
            AutomationProperties.SetName(container, $"Page {index + 1}");
            AutomationProperties.SetPositionInSet(container, index + 1);
            AutomationProperties.SetSizeOfSet(container, NumberOfPages);
        }

        private void RequestScrollToSelectedPip()
        {
            if (_scrollPending)
                return;

            _scrollPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _scrollPending = false;
                ScrollToSelectedPip();
            }, DispatcherPriority.Input);
        }

        private void ScrollToSelectedPip()
        {
            if (_pipsPagerList == null)
                return;

            var container = _pipsPagerList.ContainerFromIndex(SelectedPageIndex);
         
            if (container == null)
                return;

            // Use BringIntoView to properly scroll the selected item into view
            container.BringIntoView();
        }

        private void UpdatePagerSize()
        {
             if (_pipsPagerList == null) 
                 return;
             
             double pipSize = 12.0;

             // Try to detect the actual size from a realized container
             var container = _pipsPagerList.ContainerFromIndex(SelectedPageIndex) as Layoutable;
          
             if (container == null && _pipsPagerList.Items.Count > 0)
                 container = _pipsPagerList.ContainerFromIndex(0);

             if (container != null)
             {
                 var margin = container.Margin;
                 var size = Orientation == Orientation.Horizontal ? 
                     container.Bounds.Width + margin.Left + margin.Right : 
                     container.Bounds.Height + margin.Top + margin.Bottom;
               
                 if (size > 0) 
                     pipSize = size;
             }

             const double crossSize = 24.0;
             double spacing = 0.0;
             
             if (_pipsPagerList.ItemsPanelRoot is StackPanel itemsPanel)
             {
                 spacing = itemsPanel.Spacing;
             }

             var visibleCount = Math.Min(NumberOfPages, MaxVisiblePips);
             
             if (visibleCount <= 0)
                 return;

             var extent = (visibleCount * pipSize) + ((visibleCount - 1) * spacing);

             if (Orientation == Orientation.Horizontal)
             {
                 _pipsPagerList.Width = extent;
                 _pipsPagerList.Height = double.NaN;
             }
             else
             {
                 _pipsPagerList.Height = extent;
                 _pipsPagerList.Width = double.NaN;
             }
             
             RequestScrollToSelectedPip();
        }

        private void UpdateNavigationButtonIcons()
        {
            var isVertical = Orientation == Orientation.Vertical;
            
            if (_previousButton != null)
            {
                if (isVertical)
                    _previousButton.Classes.Add("vertical");
                else
                    _previousButton.Classes.Remove("vertical");
            }
            
            if (_nextButton != null)
            {
                if (isVertical)
                    _nextButton.Classes.Add("vertical");
                else
                    _nextButton.Classes.Remove("vertical");
            }
        }
    }
}
