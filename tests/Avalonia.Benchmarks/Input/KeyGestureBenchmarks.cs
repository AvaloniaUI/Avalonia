using System;
using System.Collections.Generic;
using Avalonia.Input;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Input
{
    [MemoryDiagnoser]
    public class KeyGestureBenchmarks
    {
        private KeyGesture _gesture = null!;
        private KeyGesture _complexGesture = null!;
        private KeyEventArgs _matchingEventArgs = null!;
        private KeyEventArgs _nonMatchingEventArgs = null!;
        private List<KeyBinding> _keyBindings = null!;

        [GlobalSetup]
        public void Setup()
        {
            _gesture = new KeyGesture(Key.S, KeyModifiers.Control);
            _complexGesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt);

            _matchingEventArgs = new KeyEventArgs
            {
                Key = Key.S,
                KeyModifiers = KeyModifiers.Control
            };

            _nonMatchingEventArgs = new KeyEventArgs
            {
                Key = Key.A,
                KeyModifiers = KeyModifiers.None
            };

            _keyBindings = new List<KeyBinding>();
            for (int i = 0; i < 20; i++)
            {
                _keyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture((Key)(i + (int)Key.A), KeyModifiers.Control)
                });
            }
        }

        [Benchmark(Baseline = true)]
        public bool KeyGesture_Matches_Simple()
        {
            return _gesture.Matches(_matchingEventArgs);
        }

        [Benchmark]
        public bool KeyGesture_Matches_Complex()
        {
            return _complexGesture.Matches(_matchingEventArgs);
        }

        [Benchmark]
        public bool KeyGesture_NoMatch()
        {
            return _gesture.Matches(_nonMatchingEventArgs);
        }

        [Benchmark]
        public KeyBinding? FindMatchingBinding_First()
        {
            foreach (var binding in _keyBindings)
            {
                if (binding.Gesture?.Matches(_matchingEventArgs) == true)
                {
                    return binding;
                }
            }
            return null;
        }

        [Benchmark]
        public KeyBinding? FindMatchingBinding_Last()
        {
            var lastEvent = new KeyEventArgs
            {
                Key = Key.T, // Last binding
                KeyModifiers = KeyModifiers.Control
            };

            foreach (var binding in _keyBindings)
            {
                if (binding.Gesture?.Matches(lastEvent) == true)
                {
                    return binding;
                }
            }
            return null;
        }

        [Benchmark]
        public KeyBinding? FindMatchingBinding_None()
        {
            foreach (var binding in _keyBindings)
            {
                if (binding.Gesture?.Matches(_nonMatchingEventArgs) == true)
                {
                    return binding;
                }
            }
            return null;
        }

        [Benchmark]
        public string KeyGesture_ToString()
        {
            return _gesture.ToString();
        }

        [Benchmark]
        public KeyGesture KeyGesture_Parse()
        {
            return KeyGesture.Parse("Ctrl+S");
        }

        [Benchmark]
        public KeyGesture KeyGesture_Parse_Complex()
        {
            return KeyGesture.Parse("Ctrl+Shift+Alt+S");
        }
    }
}
