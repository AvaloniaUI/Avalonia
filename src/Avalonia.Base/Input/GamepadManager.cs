using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Input
{
    [NotClientImplementable()]
    public interface IGamepadManager
    {
        /// <summary>
        /// Obtains the stream of gamepad events. 
        /// </summary>
        IObservable<GamepadUpdateArgs> GamepadStream { get; }
    }

    public abstract class GamepadManager : IGamepadManager
    {
        public static readonly RoutedEvent GamepadInteractionEvent;
        static GamepadManager()
        {
            GamepadInteractionEvent = RoutedEvent.Register<GamepadManager, GamepadInteractionEventArgs>(nameof(GamepadInteractionEvent), RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }

        private readonly LightweightSubject<GamepadUpdateArgs> _gamepadStream = new();
        protected readonly List<GamepadUpdateArgs> _currentState = [];

        public void PushGamepadEvent(GamepadUpdateArgs args)
        {
            if (_currentState.Count <= args.Device)
            {
                _currentState.Add(args);
            }
            else
            {
                _currentState[args.Device] = args;
            }

            // Safe to use Post, for the same priority this will preserve event order 
            Dispatcher.UIThread.Post(() =>
            {
                _gamepadStream.OnNext(args);
            });
        }

        public IObservable<GamepadUpdateArgs> GamepadStream => _gamepadStream;
        public IReadOnlyList<GamepadUpdateArgs> GetSnapshot() => [.. _currentState];

    }
}
