using System;
using System.Collections.Generic;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations
{
    
    /// <summary>
    /// Collection of composition animation key frames
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class KeyFrames<T> : List<KeyFrame<T>>, IKeyFrames
    {
        void Validate(float key)
        {
            if (key < 0 || key > 1)
                throw new ArgumentException("Key frame key");
            if (Count > 0 && this[Count - 1].NormalizedProgressKey > key)
                throw new ArgumentException("Key frame key " + key + " is less than the previous one");
        }
        
        public void InsertExpressionKeyFrame(float normalizedProgressKey, string value, IEasing easingFunction)
        {
            Validate(normalizedProgressKey);
            Add(new KeyFrame<T>
            {
                NormalizedProgressKey = normalizedProgressKey,
                Expression = Expression.Parse(value),
                EasingFunction = easingFunction
            });
        }

        public void Insert(float normalizedProgressKey, T value, IEasing easingFunction)
        {
            Validate(normalizedProgressKey);
            Add(new KeyFrame<T>
            {
                NormalizedProgressKey = normalizedProgressKey,
                Value = value,
                EasingFunction = easingFunction
            });
        }

        public ServerKeyFrame<T>[] Snapshot()
        {
            var frames = new ServerKeyFrame<T>[Count];
            for (var c = 0; c < Count; c++)
            {
                var f = this[c];
                frames[c] = new ServerKeyFrame<T>
                {
                    Expression = f.Expression,
                    Value = f.Value,
                    EasingFunction = f.EasingFunction,
                    Key = f.NormalizedProgressKey
                };
            }
            return frames;
        }
    }

    /// <summary>
    /// Composition animation key frame
    /// </summary>
    struct KeyFrame<T>
    {
        public float NormalizedProgressKey;
        public T Value;
        public Expression Expression;
        public IEasing EasingFunction;
    }
    
    /// <summary>
    /// Server-side composition animation key frame
    /// </summary>
    struct ServerKeyFrame<T>
    {
        public T Value;
        public Expression? Expression;
        public IEasing EasingFunction;
        public float Key;
    }
    
    interface IKeyFrames
    {
        public void InsertExpressionKeyFrame(float normalizedProgressKey, string value, IEasing easingFunction);
    }
}
