using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ControlCatalog.Models;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            WindowStates = new WindowState[]
            {
                WindowState.Minimized,
                WindowState.Normal,
                WindowState.Maximized,
                WindowState.FullScreen,
            };

            WindowState = WindowState.Normal;

            WindowDecorationsOptions = [.. Enum.GetValues<WindowDecorations>()];

            CurrentWindowDecorations = WindowDecorations.Full;

            CatalogThemes = [.. Enum.GetValues<CatalogTheme>()];

            WindowTransparencyLevels = new[]{
                WindowTransparencyLevel.None,
                WindowTransparencyLevel.Transparent,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Mica
            };

            CurrentWindowTransparencyLevel = WindowTransparencyLevel.None;

            FlowDirections = [.. Enum.GetValues<FlowDirection>()];

            CurrentFlowDirection = FlowDirection.LeftToRight;

            ThemeVariants = new[]
            {
                ThemeVariant.Default,
                ThemeVariant.Light,
                ThemeVariant.Dark
            };

            CurrentThemeVariant = ThemeVariant.Default;
        }

        public WindowState WindowState
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public FlowDirection CurrentFlowDirection
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }
        public ThemeVariant[] ThemeVariants { get; }
        public WindowDecorations CurrentWindowDecorations
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public CatalogTheme CurrentCatalogTheme
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public WindowTransparencyLevel CurrentWindowTransparencyLevel
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public ThemeVariant CurrentThemeVariant
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public FlowDirection[] FlowDirections { get; }
        public WindowTransparencyLevel[] WindowTransparencyLevels { get; }
        public WindowDecorations[] WindowDecorationsOptions { get; }
        public WindowState[] WindowStates { get; }
        public CatalogTheme[] CatalogThemes { get; }
    }
}
