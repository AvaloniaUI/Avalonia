using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ControlCatalog.Pages.Gamepad.Game;

namespace ControlCatalog.Pages.Gamepad
{
    public class GameDemo : Control, IObserver<GamepadUpdateArgs>
    {
        private GameWorld _world = new();
        private TopLevel? _tl;
        private TimeSpan elapsedSoFar;

        public GameDemo()
        {
            ClipToBounds = true;
            // TODO: Register systems here
            GameWorld.ActiveWorld = _world;
            _world.Add(new TestPlayerEntity());
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(GamepadUpdateArgs value)
        {
            _world.DispatchGamepadInput(value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _tl = TopLevel.GetTopLevel(this);
            _tl?.RequestAnimationFrame(OnTick);
            TopLevel.GetTopLevel(this)?.GamepadManager?.GamepadStream.Subscribe(this);
            
            base.OnAttachedToVisualTree(e);
        }

        private void OnTick(TimeSpan elapsed)
        {

            _world.DispatchTick(elapsed - elapsedSoFar);
            elapsedSoFar = elapsed;

            InvalidateVisual();

            _tl?.RequestAnimationFrame(OnTick);
        }

        public override void Render(DrawingContext context)
        {
            _world.DispatchRender(context);

            base.Render(context);
        }
    }
}
