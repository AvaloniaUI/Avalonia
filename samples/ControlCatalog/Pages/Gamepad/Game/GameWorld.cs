using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public class GameWorld : IWorld
    {
        private List<IExecutionSystem> _systems = new(); 
        private List<IRenderSystem> _renderSystems = new();
        private HashSet<IEntity> _entities = new();
        private List<IEntity> _entitiesWithDeferredDeletion = new();
        private List<IEntity> _entitiesWithDeferredAddition = new();
        private List<IGamepadSystem> _gamepadSystems = new();

        public void DeregisterSystem(IExecutionSystem system)
        {
            _systems.Add(system);
            if (system is IRenderSystem irs)
            {
                _renderSystems.Remove(irs);
            }
            if (system is IGamepadSystem igs)
            {
                _gamepadSystems.Remove(igs);
            }
        }

        public void DispatchGamepadInput(GamepadUpdateArgs args)
        {
            foreach (var igs in _gamepadSystems)
            {
                igs.GamepadInput(args);
            }
        }

        public void DispatchRender(DrawingContext context)
        {
            foreach(var system in _renderSystems)
            {
                system.Render(context);
            }
        }

        public void DispatchTick(TimeSpan elapsed)
        {
            foreach (var system in _systems)
            {
                system.Tick(elapsed);
            }

            foreach (var entity in _entitiesWithDeferredDeletion)
            {
                _entities.Remove(entity);
            } 
            foreach (var entity in _entitiesWithDeferredAddition)
            {
                _entities.Add(entity);
            }
        }



        public void OnAdd(IEntity entity)
        {
            _entitiesWithDeferredAddition.Add(entity);
        }

        public void OnRemove(IEntity entity)
        {
            _entitiesWithDeferredDeletion.Add(entity);
        }

        public void RegisterSystem(IExecutionSystem system)
        {
            _systems.Add(system);
            if (system is IRenderSystem irs)
            {
                _renderSystems.Add(irs);
            }
            if (system is IGamepadSystem igs)
            {
                _gamepadSystems.Add(igs);
            }
        }
    }
}
