using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public abstract class GameObjectBase
    {
        private double _mass = 1.0d;

        /// <summary>
        /// The Position of the object. Should not be directly modified by others. 
        /// </summary>
        public Vector Position { get; set; }
        /// <summary>
        /// The Velocity of the object. Should not be directly modified by others. 
        /// </summary>
        public Vector Velocity { get; set; }
        /// <summary>
        /// The mass of the object. Never set this to zero. 
        /// </summary>
        public double Mass
        {
            get => _mass;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Mass must be positive.");
                _mass = value;
            }
        }
        /// <summary>
        /// The relative hitbox of the object. 
        /// </summary>
        public Rect Hitbox { get; set; }

        /// <summary>
        /// The type of physics that this object has. Defaults to <see cref="ObjectPhysicsType.Static"/>
        /// </summary>
        public ObjectPhysicsType PhysicsType { get; set; }

        public QuadTreeNode? CurrentNode { get; set; }

        public virtual void Update(TimeSpan deltaTime)
        {
            Vector oldPosition = Position;
            Vector newPosition = Position + deltaTime.TotalSeconds * Velocity;

            GameWorld.ActiveWorld.IssueMoveRequest(this, newPosition);

            Position = newPosition;

            GameWorld.ActiveWorld.FinalizeMovement(this);
        }

        public virtual void ApplyForce(Vector force)
        {
            if (PhysicsType != ObjectPhysicsType.Static)
            {
                Velocity += force / Mass;
            }
        }

        public virtual void ResolveCollision(GameObjectBase other)
        {

        }

        public virtual void OnRender(DrawingContext context)
        {

        }

        public virtual void OnCreate()
        {

        }

        public virtual void OnDestroy()
        {
            
        }

        public virtual void OnCollisionEnter(GameObjectBase other)
        {

        }

        public virtual void OnCollisionExit(GameObjectBase other)
        {

        }

        public bool IsPhysicsType(ObjectPhysicsType checkingType)
        {
            return (PhysicsType & checkingType) == checkingType;
        }
    }

    [Flags]
    public enum ObjectPhysicsType
    {
        /// <summary>No special physics behavior</summary>
        None = 0,
        /// <summary>
        /// Requires mutual exclusion of its space (collides with other SpaceOccupying objects)
        /// </summary>
        SpaceOccupying = 1,
        /// <summary>
        /// // Can coexist in the same space as others (no collision with anything)
        /// </summary>
        NonSpaceOccupying = 2,
        /// <summary>
        /// Immovable, unaffected by forces
        /// </summary>
        Static = 4,
        /// <summary>
        /// Responds to forces (implies movable)
        /// </summary>
        Dynamic = 8,
        // I had an idea of something that moves that blocks you but doesn't interact with forces
        // but that's probably just SpaceOccupying with no other flags
    }
}
