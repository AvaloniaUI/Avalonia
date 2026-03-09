using System;
using Avalonia.Threading;
using Avalonia.Controls.Metadata;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using System.Collections.Generic;

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
        private const string PART_PreviousButton = "PART_PreviousButton";
        private const string PART_NextButton = "PART_NextButton";
        private const string PART_PipsPagerList = "PART_PipsPagerList";

        private Button? _previousButton;
        private Button? _nextButton;
        private ItemsControl? _pipsPagerList;
        private bool _scrollPending;
        private bool _updatingPagerSize;
        private PipsPagerTemplateSettings _templateSettings = new PipsPagerTemplateSettings();

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
            AvaloniaProperty.Register<PipsPager, int>(nameof(SelectedPageIndex),
                defaultBindingMode: BindingMode.TwoWay);
        
        /// <summary>
        /// Defines the <see cref="TemplateSettings"/> property.
        /// </summary>
        public static readonly DirectProperty<PipsPager, PipsPagerTemplateSettings> TemplateSettingsProperty =
            AvaloniaProperty.RegisterDirect<PipsPager, PipsPagerTemplateSettings>(nameof(TemplateSettings),
                x => x.TemplateSettings);

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
        /// Defines the <see cref="SelectedIndexChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<PipsPagerSelectedIndexChangedEventArgs> SelectedIndexChangedEvent =
            RoutedEvent.Register<PipsPager, PipsPagerSelectedIndexChangedEventArgs>(nameof(SelectedIndexChanged), RoutingStrategies.Bubble);

        /// <summary>
        /// Occurs when the selected index has changed.
        /// </summary>
        public event EventHandler<PipsPagerSelectedIndexChangedEventArgs>? SelectedIndexChanged
        {
            add => AddHandler(SelectedIndexChangedEvent, value);
            remove => RemoveHandler(SelectedIndexChangedEvent, value);
        }

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
            get => _templateSettings;
            private set => SetAndRaise(TemplateSettingsProperty, ref _templateSettings, value);
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
            _previousButton = e.NameScope.Find<Button>(PART_PreviousButton);
            _nextButton = e.NameScope.Find<Button>(PART_NextButton);
            _pipsPagerList = e.NameScope.Find<ItemsControl>(PART_PipsPagerList);

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

            var isHorizontal = Orientation == Orientation.Horizontal;

            switch (e.Key)
            {
                case Key.Left when isHorizontal:
                case Key.Up when !isHorizontal:
                    if (SelectedPageIndex > 0)
                    {
                        SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex - 1);
                        e.Handled = true;
                    }
                    break;
                case Key.Right when isHorizontal:
                case Key.Down when !isHorizontal:
                    if (SelectedPageIndex < NumberOfPages - 1)
                    {
                        SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex + 1);
                        e.Handled = true;
                    }
                    break;
                case Key.Home:
                    SetCurrentValue(SelectedPageIndexProperty, 0);
                    e.Handled = true;
                    break;
                case Key.End:
                    if (NumberOfPages > 0)
                    {
                        SetCurrentValue(SelectedPageIndexProperty, NumberOfPages - 1);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void OnSelectedPageIndexChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newIndex = e.GetNewValue<int>();
            var oldIndex = e.GetOldValue<int>();

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

            RaiseEvent(new PipsPagerSelectedIndexChangedEventArgs(oldIndex, newIndex));
        }

        private void OnNumberOfPagesChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = e.GetNewValue<int>();

            if (newValue < 0)
            {
                SetCurrentValue(NumberOfPagesProperty, 0);
                return;
            }

            var pips = TemplateSettings.Pips;

            if (pips.Count < newValue)
            {
                var start = pips.Count + 1;
                var count = newValue - pips.Count;
                var toAdd = new List<int>(count);
                for (int i = 0; i < count; i++)
                {
                    toAdd.Add(start + i);
                }
                pips.AddRange(toAdd);
            }
            else if (pips.Count > newValue)
            {
                pips.RemoveRange(newValue, pips.Count - newValue);
            }

            if (newValue > 0 && SelectedPageIndex >= newValue)
            {
                SetCurrentValue(SelectedPageIndexProperty, newValue - 1);
            }
            else if (newValue == 0 && SelectedPageIndex > 0)
            {
                SetCurrentValue(SelectedPageIndexProperty, 0);
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
        }

        private void OnMaxVisiblePipsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdatePagerSize();
        }

        private void PreviousButton_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedPageIndex > 0)
            {
                SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex - 1);
            }
        }

        private void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedPageIndex < NumberOfPages - 1)
            {
                SetCurrentValue(SelectedPageIndexProperty, SelectedPageIndex + 1);
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
            if (!_updatingPagerSize)
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

             _updatingPagerSize = true;
             
             try
             {
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
             finally
             {
                 _updatingPagerSize = false;
             }
        }



    }
}
