#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avalonia.X11.Screens;

internal partial class X11Screens
{
    interface IScalingProvider
    {
        double GetScaling(X11Screen screen, int index);
    }

    interface IScalingProviderWithChanges : IScalingProvider
    {
        event Action SettingsChanged;
    }

    class PostMultiplyScalingProvider : IScalingProvider
    {
        private readonly IScalingProvider _inner;
        private readonly double _factor;

        public PostMultiplyScalingProvider(IScalingProvider inner, double factor)
        {
            _inner = inner;
            _factor = factor;
        }

        public double GetScaling(X11Screen screen, int index) => _inner.GetScaling(screen, index) * _factor;
    }

    class NullScalingProvider : IScalingProvider
    {
        public double GetScaling(X11Screen screen, int index) => 1;
    }


    class XrdbScalingProvider : IScalingProviderWithChanges
    {
        private readonly XResources _resources;
        private double _factor = 1;

        public XrdbScalingProvider(AvaloniaX11Platform platform)
        {
            _resources = platform.Resources;
            _resources.ResourceChanged += name =>
            {
                if (name == "Xft.dpi")
                    Update();
            };
            Update();
        }

        void Update()
        {
            var factor = 1d;
            var stringValue = _resources.GetResource("Xft.dpi")?.Trim();
            if (!string.IsNullOrWhiteSpace(stringValue) && double.TryParse(stringValue, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var parsed))
            {
                factor = parsed / 96;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_factor != factor)
            {
                _factor = factor;
                SettingsChanged?.Invoke();
            }
        }
        
        public event Action? SettingsChanged;
        
        public double GetScaling(X11Screen screen, int index) => _factor;
    }
    
    class PhysicalDpiScalingProvider : IScalingProvider
    {
        private const int FullHDWidth = 1920;
        private const int FullHDHeight = 1080;
        
        public double GetScaling(X11Screen screen, int index)
        {
            if (screen.PhysicalSize == null)
                return 1;
            return GuessPixelDensity(screen.Bounds, screen.PhysicalSize.Value);
        }

        double GuessPixelDensity(PixelRect pixel, Size physical)
        {
            var calculatedDensity = 1d;
            if (physical.Width > 0)
                calculatedDensity = pixel.Width <= FullHDWidth
                    ? 1
                    : Math.Max(1, pixel.Width / physical.Width * 25.4 / 96);
            else if (physical.Height > 0)
                calculatedDensity = pixel.Height <= FullHDHeight
                    ? 1
                    : Math.Max(1, pixel.Height / physical.Height * 25.4 / 96);

            if (calculatedDensity > 3)
                return 1;
            else
            {
                var sanePixelDensities = new double[] { 1, 1.25, 1.50, 1.75, 2 };
                foreach (var saneDensity in sanePixelDensities)
                {
                    if (calculatedDensity <= saneDensity + 0.20)
                        return saneDensity;
                }

                return sanePixelDensities.Last();
            }
        }
    }

    class UserConfiguredScalingProvider : IScalingProvider
    {
        private readonly Dictionary<string, double>? _namedConfig;
        private readonly List<double>? _indexedConfig;
        

        public UserConfiguredScalingProvider(Dictionary<string, double>? namedConfig, List<double>? indexedConfig)
        {
            _namedConfig = namedConfig;
            _indexedConfig = indexedConfig;
        }
        
        public double GetScaling(X11Screen screen, int index)
        {
            if (_indexedConfig != null)
            {
                if (index > 0 && index < _indexedConfig.Count)
                    return _indexedConfig[index];
                return 1;
            }
            if (_namedConfig?.TryGetValue(screen.Name, out var scaling) == true)
                return scaling;

            return 1;
        }
    }
    
    class UserScalingConfiguration
    {
        public Dictionary<string, double>? NamedConfig { get; set; }
        public List<double>? IndexedConfig { get; set; }
    }
    
    static (UserScalingConfiguration? config, double global, bool forceAuto)? TryGetEnvConfiguration(
        string globalFactorName, string userConfigName, string[] autoNames)
    {
        var globalFactorString = Environment.GetEnvironmentVariable(globalFactorName);
        var screenFactorsString = Environment.GetEnvironmentVariable(userConfigName);
        bool usePhysicalDpi = false;
        foreach (var autoName in autoNames)
        {
            var envValue = Environment.GetEnvironmentVariable(autoName);
            if (envValue == "1")
                usePhysicalDpi = true;
        }

        double? globalFactor = null;
        if (!string.IsNullOrWhiteSpace(globalFactorString) 
            && double.TryParse(globalFactorString, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            globalFactor = parsed;

        UserScalingConfiguration? userConfig = null;
        if (!string.IsNullOrWhiteSpace(screenFactorsString))
        {
            try
            {
                var split = screenFactorsString.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                if (split[0].Contains('='))
                {
                    userConfig = new UserScalingConfiguration
                    {
                        NamedConfig = split.Select(x => x.Split(new[] { '=' }, 2))
                            .ToDictionary(x => x[0], x => double.Parse(x[1], CultureInfo.InvariantCulture))
                    };
                }
                else
                {
                    userConfig = new UserScalingConfiguration
                    {
                        IndexedConfig = split.Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToList()
                    };
                }
            }
            catch
            {
                Console.Error.WriteLine($"Unable to parse {userConfigName}={screenFactorsString}");
            }
        }
        
        
        if (globalFactorString == null && screenFactorsString == null)
            return null;

        return (userConfig, globalFactor ?? 1, usePhysicalDpi);
    }
    
    
    static IScalingProvider GetScalingProvider(AvaloniaX11Platform platform)
    {
        var envSets = new[]
        {
            ("AVALONIA_GLOBAL_SCALE_FACTOR", "AVALONIA_SCREEN_SCALE_FACTORS", new[] { "AVALONIA_USE_PHYSICAL_DPI" })
        }.ToList();

        if (Environment.GetEnvironmentVariable("AVALONIA_SCREEN_SCALE_IGNORE_QT") != "1")
        {
            envSets.Add(("QT_SCALE_FACTOR", "QT_SCREEN_SCALE_FACTORS",
                new[] { "QT_AUTO_SCREEN_SCALE_FACTOR", "QT_USE_PHYSICAL_DPI" }));
        }

        UserScalingConfiguration? config = null;
        double global = 1;
        bool forceAuto = false;

        
        foreach (var envSet in envSets)
        {
            var envConfig = TryGetEnvConfiguration(envSet.Item1, envSet.Item2, envSet.Item3);
            if (envConfig != null)
            {
                (config, global, forceAuto) = envConfig.Value;
                break;
            }
        }

        IScalingProvider provider;
        if (config != null)
            provider = new UserConfiguredScalingProvider(config.NamedConfig, config.IndexedConfig);
        else if (forceAuto)
            provider = new PhysicalDpiScalingProvider();
        else 
            provider = new XrdbScalingProvider(platform);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (global != 1)
            provider = new PostMultiplyScalingProvider(provider, global);

        return provider;
    }
}
