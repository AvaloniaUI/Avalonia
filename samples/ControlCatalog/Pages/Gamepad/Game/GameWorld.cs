using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public class GameWorld : IWorld
    {
        private static GameWorld? s_activeWorld;
        public static GameWorld ActiveWorld
        {
            get => s_activeWorld ?? throw new Exception("NO ACTIVE WORLD"); set => s_activeWorld = value;
        }

        private HashSet<GameObjectBase> _objects = new();
        private List<GameObjectBase> _entitiesWithDeferredDeletion = new();
        private List<GameObjectBase> _entitiesWithDeferredAddition = new();
        private QuadTreeNode _quadTree = new(new Rect(-524288, -524288, 1048576, 1048576), 50, 50, 0);

        public event Action<GamepadUpdateArgs>? GamepadUpdate;

        public void DispatchGamepadInput(GamepadUpdateArgs args)
        {
            GamepadUpdate?.Invoke(args);
        }

        public void IssueMoveRequest(GameObjectBase obj, Vector newPosition)
        {
            foreach(var other in _quadTree.Retrieve(obj.Hitbox.Translate(newPosition)))
            {
                obj.ResolveCollision(other);
            }
        }

        public void FinalizeMovement(GameObjectBase obj)
        {
            _quadTree.Upsert(obj);
        }

        public void DispatchRender(DrawingContext context)
        {
            foreach (var obj in _objects)
            {
                obj.OnRender(context);
            }
        }

        public void DispatchTick(TimeSpan elapsed)
        {
            foreach (var obj in _objects)
            {
                obj.Update(elapsed);
            }

            foreach (var entity in _entitiesWithDeferredDeletion)
            {
                _objects.Remove(entity);
                _quadTree.Remove(entity);
                entity.OnDestroy();
            }
            _entitiesWithDeferredDeletion.Clear();
            foreach (var entity in _entitiesWithDeferredAddition)
            {
                _objects.Add(entity);
                _quadTree.Insert(entity);
                entity.OnCreate();
            }
            _entitiesWithDeferredAddition.Clear();
        }

        public void Add(GameObjectBase obj)
        {
            _entitiesWithDeferredAddition.Add(obj);
        }

        public void Destroy(GameObjectBase obj)
        {
            _entitiesWithDeferredDeletion.Add(obj);
        }
    }
}
