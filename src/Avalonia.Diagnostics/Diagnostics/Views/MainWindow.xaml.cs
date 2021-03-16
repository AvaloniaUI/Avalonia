﻿using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.Views
{
    internal class MainWindow : Window, IStyleHost
    {
        private readonly IDisposable _keySubscription;
        private TopLevel _root;

        public MainWindow()
        {
            InitializeComponent();

            _keySubscription = InputManager.Instance.Process
                .OfType<RawKeyEventArgs>()
                .Subscribe(RawKeyDown);
        }

        public TopLevel Root
        {
            get => _root;
            set
            {
                if (_root != value)
                {
                    if (_root != null)
                    {
                        _root.Closed -= RootClosed;
                    }

                    _root = value;

                    if (_root != null)
                    {
                        _root.Closed += RootClosed;
                        DataContext = new MainViewModel(value);
                    }
                    else
                    {
                        DataContext = null;
                    }
                }
            }
        }

        IStyleHost IStyleHost.StylingParent => null;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _keySubscription.Dispose();
            _root.Closed -= RootClosed;
            _root = null;
            ((MainViewModel)DataContext)?.Dispose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void RawKeyDown(RawKeyEventArgs e)
        {
            const RawInputModifiers modifiers = RawInputModifiers.Control | RawInputModifiers.Shift;

            if (e.Modifiers == modifiers)
            {
                var point = (Root as IInputRoot)?.MouseDevice?.GetPosition(Root) ?? default;
                
                var control = Root.GetVisualsAt(point, x =>
                    {
                        if (x is AdornerLayer || !x.IsVisible) return false;
                        if (!(x is IInputElement ie)) return true;
                        return ie.IsHitTestVisible;
                    })
                    .FirstOrDefault();

                if (control != null)
                {
                    var vm = (MainViewModel)DataContext;
                    vm.SelectControl((IControl)control);
                }
            } 
            else if (e.Modifiers == RawInputModifiers.Alt)
            {
                if (e.Key == Key.S || e.Key == Key.D)
                {
                    var enable = e.Key == Key.S;

                    var vm = (MainViewModel)DataContext;
                    vm.EnableSnapshotStyles(enable);
                }
            }
        }

        private void RootClosed(object sender, EventArgs e) => Close();
    }
}
