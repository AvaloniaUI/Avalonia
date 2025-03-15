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
        /// Obtains a stream of gamepad events. 
        /// </summary>
        IObservable<GamepadEventArgs> GamepadStream { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<GamepadState> GetSnapshot();
    }

    public abstract class GamepadManager : IGamepadManager
    {
        /// <summary>
        /// The static entry-point to interacting with Gamepads. This is null if the current platform does not have a Gamepad implementation. 
        /// </summary>
        public static GamepadManager? Instance { get; protected set; }

        public static readonly RoutedEvent GamepadStateChanged;
        static GamepadManager()
        {
            GamepadStateChanged = RoutedEvent.Register<GamepadManager, GamepadEventArgs>(nameof(GamepadStateChanged), RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }

        private readonly LightweightSubject<GamepadEventArgs> _gamepadStream = new();
        protected readonly List<GamepadState> _currentState = [];

        public void PushGamepadEvent(GamepadEventArgs args)
        {
            if (_currentState.Count <= args.Device)
            {
                _currentState.Add(args.State);
            }
            else
            {
                _currentState[args.Device] = args.State;
            }

            // question - if I use Post instead of Invoke will events end up out of order?
            Dispatcher.UIThread.Post(() =>
            {
                _gamepadStream.OnNext(args);
            });
        }

        public IObservable<GamepadEventArgs> GamepadStream => _gamepadStream;
        public IReadOnlyList<GamepadState> GetSnapshot() => [.. _currentState];

    }
}
