using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// <see cref="CompositionPropertySet"/>s are <see cref="CompositionObject"/>s that allow storage of key values pairs
    /// that can be shared across the application and are not tied to the lifetime of another composition object.
    /// <see cref="CompositionPropertySet"/>s are most commonly used with animations, where they maintain key-value pairs
    /// that are referenced to drive portions of composition animations. <see cref="CompositionPropertySet"/>s
    /// provide the ability to insert key-value pairs or retrieve a value for a given key.
    /// <see cref="CompositionPropertySet"/> does not support a delete function – ensure you use <see cref="CompositionPropertySet"/>
    /// to store values that will be shared across the application.
    /// </summary>
    public sealed class CompositionPropertySet : CompositionObject
    {
        private readonly Dictionary<string, ExpressionVariant> _variants = new Dictionary<string, ExpressionVariant>();
        private readonly Dictionary<string, CompositionObject> _objects = new Dictionary<string, CompositionObject>();
        
        internal CompositionPropertySet(Compositor compositor) : base(compositor, null)
        {
        }

        internal void Set(string key, ExpressionVariant value)
        {
            _objects.Remove(key);
            _variants[key] = value;
        }

        /*
         For INTERNAL USE by CompositionAnimation ONLY, we DON'T support expression
         paths like SomeParam.SomePropertyObject.SomeValue
        */
        internal void Set(string key, CompositionObject obj)
        {
            _objects[key] = obj ?? throw new ArgumentNullException(nameof(obj));
            _variants.Remove(key);
        }
        
        public void InsertColor(string propertyName, Avalonia.Media.Color value) => Set(propertyName, value);

        public void InsertMatrix3x2(string propertyName, Matrix3x2 value) => Set(propertyName, value);

        public void InsertMatrix4x4(string propertyName, Matrix4x4 value) => Set(propertyName, value);

        public void InsertQuaternion(string propertyName, Quaternion value) => Set(propertyName, value);

        public void InsertScalar(string propertyName, float value) => Set(propertyName, value);
        public void InsertVector2(string propertyName, Vector2 value) => Set(propertyName, value);

        public void InsertVector3(string propertyName, Vector3 value) => Set(propertyName, value);

        public void InsertVector4(string propertyName, Vector4 value) => Set(propertyName, value);


        CompositionGetValueStatus TryGetVariant<T>(string key, out T value) where T : struct
        {
            value = default;
            if (!_variants.TryGetValue(key, out var v))
                return _objects.ContainsKey(key)
                    ? CompositionGetValueStatus.TypeMismatch
                    : CompositionGetValueStatus.NotFound;

            return v.TryCast(out value) ? CompositionGetValueStatus.Succeeded : CompositionGetValueStatus.TypeMismatch;
        }

        public CompositionGetValueStatus TryGetColor(string propertyName, out Avalonia.Media.Color value) 
            => TryGetVariant(propertyName, out value);

        public CompositionGetValueStatus TryGetMatrix3x2(string propertyName, out Matrix3x2 value)
            => TryGetVariant(propertyName, out value);

        public CompositionGetValueStatus TryGetMatrix4x4(string propertyName, out Matrix4x4 value)
            => TryGetVariant(propertyName, out value);

        public CompositionGetValueStatus TryGetQuaternion(string propertyName, out Quaternion value)
            => TryGetVariant(propertyName, out value);

        
        public CompositionGetValueStatus TryGetScalar(string propertyName, out float value)
            => TryGetVariant(propertyName, out value);

        public CompositionGetValueStatus TryGetVector2(string propertyName, out Vector2 value)
            => TryGetVariant(propertyName, out value);

        public CompositionGetValueStatus TryGetVector3(string propertyName, out Vector3 value)
            => TryGetVariant(propertyName, out value);

        public CompositionGetValueStatus TryGetVector4(string propertyName, out Vector4 value)
            => TryGetVariant(propertyName, out value);


        public void InsertBoolean(string propertyName, bool value) => Set(propertyName, value);

        public CompositionGetValueStatus TryGetBoolean(string propertyName, out bool value)
            => TryGetVariant(propertyName, out value);

        internal void ClearAll()
        {
            _objects.Clear();
            _variants.Clear();
        }

        internal void Clear(string key)
        {
            _objects.Remove(key);
            _variants.Remove(key);
        }

        internal PropertySetSnapshot Snapshot() =>
            SnapshotCore(1);
        
        private PropertySetSnapshot SnapshotCore(int allowedNestingLevel)
        {
            var dic = new Dictionary<string, PropertySetSnapshot.Value>(_objects.Count + _variants.Count);
            foreach (var o in _objects)
            {
                if (o.Value is CompositionPropertySet ps)
                {
                    if (allowedNestingLevel <= 0)
                        throw new InvalidOperationException("PropertySet depth limit reached");
                    dic[o.Key] = new PropertySetSnapshot.Value(ps.SnapshotCore(allowedNestingLevel - 1));
                }
                else if (o.Value.Server == null)
                    throw new InvalidOperationException($"Object of type {o.Value.GetType()} is not allowed");
                else
                    dic[o.Key] = new PropertySetSnapshot.Value((ServerObject)o.Value.Server);
            }

            foreach (var v in _variants)
                dic[v.Key] = v.Value;
            
            return new PropertySetSnapshot(dic);
        }
    }

    public enum CompositionGetValueStatus
    {
        Succeeded,
        TypeMismatch,
        NotFound
    }
}
